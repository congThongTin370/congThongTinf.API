using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using congThongTinf.Service.DTOs;
using congThongTinf.Service.Interfaces;
using congThongTinf.Service.Services.Commons;
using congThongTinf.Service.Utils;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace congThongTinf.Service.Services;

public class CloudflareOptions
{
    public const string SectionName = "CloudflareR2";

    public string AccountId { get; set; } = string.Empty;

    public string ApiToken { get; set; } = string.Empty;

    public string BucketName { get; set; } = string.Empty;

    public string Jurisdiction { get; set; } = string.Empty;

    public long StorageQuotaBytes { get; set; } = 10L * 1024 * 1024 * 1024;
}

public class R2StorageAnalyticsService : IR2StorageAnalyticsService
{
    private static readonly TimeSpan Lookback = TimeSpan.FromDays(7);

    // GraphQL built-in scalar is String. The R2 docs sample uses lowercase string, which the API rejects.
    private const string StorageQuery = """
        query R2StorageUsage($accountTag: String!, $startDate: Time!, $endDate: Time!, $bucketName: String!) {
          viewer {
            accounts(filter: { accountTag: $accountTag }) {
              r2StorageAdaptiveGroups(
                limit: 1
                filter: {
                  datetime_geq: $startDate
                  datetime_leq: $endDate
                  bucketName: $bucketName
                }
                orderBy: [datetime_DESC]
              ) {
                max {
                  objectCount
                  payloadSize
                  metadataSize
                }
                dimensions {
                  datetime
                }
              }
            }
          }
        }
        """;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private readonly HttpClient _httpClient;
    private readonly CloudflareOptions _analytics;
    private readonly CloudflareR2Options _r2;
    private readonly ILogger<R2StorageAnalyticsService> _logger;

    public R2StorageAnalyticsService(
        HttpClient httpClient,
        IOptions<CloudflareOptions> analyticsOptions,
        IOptions<CloudflareR2Options> r2Options,
        ILogger<R2StorageAnalyticsService> logger)
    {
        _httpClient = httpClient;
        _analytics = analyticsOptions.Value;
        _r2 = r2Options.Value;
        _logger = logger;
    }

    public async Task<R2StorageUsageDto> GetUsageAsync(CancellationToken cancellationToken = default)
    {
        var accountId = _analytics.AccountId?.Trim() ?? string.Empty;
        var apiToken = _analytics.ApiToken?.Trim() ?? string.Empty;
        var bucketName = ResolveBucketName();
        var quotaBytes = _analytics.StorageQuotaBytes;

        if (string.IsNullOrEmpty(accountId) || string.IsNullOrEmpty(apiToken))
            throw ErrorHelper.Internal("Thiếu AccountId hoặc ApiToken của Cloudflare Analytics.");

        if (string.IsNullOrEmpty(bucketName))
            throw ErrorHelper.Internal("Chưa cấu hình tên bucket R2.");

        if (quotaBytes <= 0)
            throw ErrorHelper.Internal("Hạn mức lưu trữ R2 chưa được cấu hình.");

        var end = DateTime.UtcNow;
        var start = end - Lookback;
        var metricsBucketName = ApplyJurisdiction(bucketName, _analytics.Jurisdiction);

        _logger.LogInformation(
            "Querying R2 storage analytics for bucket {Bucket} from {Start:o} to {End:o}.",
            metricsBucketName,
            start,
            end);

        GraphQlEnvelope envelope;
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, "graphql");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiToken);
            request.Content = JsonContent.Create(new
            {
                query = StorageQuery,
                variables = new
                {
                    accountTag = accountId,
                    startDate = start.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                    endDate = end.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                    bucketName = metricsBucketName
                }
            }, options: JsonOptions);

            using var response = await _httpClient.SendAsync(request, cancellationToken);
            var body = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning(
                    "Cloudflare Analytics returned HTTP {StatusCode} for bucket {Bucket}.",
                    (int)response.StatusCode,
                    metricsBucketName);
                throw ErrorHelper.WithStatus(502, DescribeHttpFailure(response.StatusCode, body));
            }

            envelope = JsonSerializer.Deserialize<GraphQlEnvelope>(body, JsonOptions)
                       ?? throw ErrorHelper.WithStatus(502, "Cloudflare Analytics trả phản hồi rỗng.");
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException)
        {
            if (ex is TaskCanceledException && cancellationToken.IsCancellationRequested)
                throw;

            _logger.LogError(ex, "Cloudflare Analytics request failed for bucket {Bucket}.", metricsBucketName);
            throw ErrorHelper.WithStatus(502, "Không gọi được Cloudflare Analytics.");
        }

        if (envelope.Errors is { Count: > 0 })
        {
            var message = envelope.Errors[0].Message;
            _logger.LogWarning("Cloudflare Analytics GraphQL error for bucket {Bucket}: {Message}", metricsBucketName, message);
            throw ErrorHelper.WithStatus(502, string.IsNullOrWhiteSpace(message)
                ? "Cloudflare Analytics trả lỗi GraphQL."
                : $"Cloudflare Analytics: {message}");
        }

        var account = envelope.Data?.Viewer?.Accounts?.FirstOrDefault();
        if (account == null)
            throw ErrorHelper.WithStatus(502, "Không tìm thấy tài khoản Cloudflare tương ứng với AccountId.");

        var sample = account.R2StorageAdaptiveGroups?.FirstOrDefault();
        if (sample?.Max == null)
            throw ErrorHelper.NotFound("Cloudflare chưa trả số liệu lưu trữ cho bucket này trong 7 ngày gần nhất.");

        var payloadBytes = sample.Max.PayloadSize;
        var metadataBytes = sample.Max.MetadataSize;
        var objectCount = sample.Max.ObjectCount;
        if (payloadBytes < 0 || metadataBytes < 0 || objectCount < 0)
            throw ErrorHelper.WithStatus(502, "Cloudflare Analytics trả số liệu lưu trữ không hợp lệ.");

        var usedBytes = payloadBytes + metadataBytes;
        var remainingBytes = usedBytes >= quotaBytes ? 0 : quotaBytes - usedBytes;
        var usedPercent = Math.Round(usedBytes * 100d / quotaBytes, 2, MidpointRounding.AwayFromZero);

        DateTime? measuredAt = null;
        if (!string.IsNullOrWhiteSpace(sample.Dimensions?.Datetime)
            && DateTime.TryParse(sample.Dimensions.Datetime, null, System.Globalization.DateTimeStyles.RoundtripKind, out var parsed))
        {
            measuredAt = parsed.ToUniversalTime();
        }

        return new R2StorageUsageDto
        {
            BucketName = metricsBucketName,
            PayloadBytes = payloadBytes,
            MetadataBytes = metadataBytes,
            UsedBytes = usedBytes,
            ObjectCount = objectCount,
            QuotaBytes = quotaBytes,
            RemainingBytes = remainingBytes,
            UsedPercent = usedPercent,
            MeasuredAt = measuredAt
        };
    }

    private string ResolveBucketName()
    {
        if (!string.IsNullOrWhiteSpace(_analytics.BucketName))
            return _analytics.BucketName.Trim();

        return _r2.BucketName?.Trim() ?? string.Empty;
    }

    private static string ApplyJurisdiction(string bucketName, string? jurisdiction)
    {
        var prefix = jurisdiction?.Trim();
        if (string.IsNullOrEmpty(prefix))
            return bucketName;

        return $"{prefix}_{bucketName}";
    }

    private static string DescribeHttpFailure(System.Net.HttpStatusCode statusCode, string body)
    {
        if (statusCode is System.Net.HttpStatusCode.Unauthorized or System.Net.HttpStatusCode.Forbidden)
            return "Cloudflare từ chối token analytics. Token cần quyền Account Analytics Read.";

        var graphMessage = TryReadGraphQlMessage(body);
        if (!string.IsNullOrWhiteSpace(graphMessage))
            return $"Cloudflare Analytics: {graphMessage}";

        return $"Cloudflare Analytics trả lỗi HTTP {(int)statusCode}.";
    }

    private static string? TryReadGraphQlMessage(string body)
    {
        try
        {
            var envelope = JsonSerializer.Deserialize<GraphQlEnvelope>(body, JsonOptions);
            return envelope?.Errors?.FirstOrDefault()?.Message;
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private sealed class GraphQlEnvelope
    {
        public GraphQlData? Data { get; set; }

        public List<GraphQlError>? Errors { get; set; }
    }

    private sealed class GraphQlData
    {
        public GraphQlViewer? Viewer { get; set; }
    }

    private sealed class GraphQlViewer
    {
        public List<GraphQlAccount>? Accounts { get; set; }
    }

    private sealed class GraphQlAccount
    {
        public List<StorageGroup>? R2StorageAdaptiveGroups { get; set; }
    }

    private sealed class StorageGroup
    {
        public StorageMax? Max { get; set; }

        public StorageDimensions? Dimensions { get; set; }
    }

    private sealed class StorageMax
    {
        public long ObjectCount { get; set; }

        public long PayloadSize { get; set; }

        public long MetadataSize { get; set; }
    }

    private sealed class StorageDimensions
    {
        public string? Datetime { get; set; }
    }

    private sealed class GraphQlError
    {
        public string? Message { get; set; }
    }
}
