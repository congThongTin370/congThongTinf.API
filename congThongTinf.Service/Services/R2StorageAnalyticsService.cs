using Amazon.S3;
using Amazon.S3.Model;
using congThongTinf.Service.DTOs;
using congThongTinf.Service.Interfaces;
using congThongTinf.Service.Services.Commons;
using congThongTinf.Service.Utils;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace congThongTinf.Service.Services;

public class R2StorageAnalyticsService : IR2StorageAnalyticsService
{
    private readonly IAmazonS3 _s3Client;
    private readonly ObjectStorageOptions _options;
    private readonly ILogger<R2StorageAnalyticsService> _logger;

    public R2StorageAnalyticsService(
        IAmazonS3 s3Client,
        IOptions<ObjectStorageOptions> options,
        ILogger<R2StorageAnalyticsService> logger)
    {
        _s3Client = s3Client;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<R2StorageUsageDto> GetUsageAsync(CancellationToken cancellationToken = default)
    {
        var bucketName = _options.BucketName?.Trim() ?? string.Empty;
        var quotaBytes = _options.StorageQuotaBytes;

        if (string.IsNullOrEmpty(bucketName))
            throw ErrorHelper.Internal("Chưa cấu hình tên bucket lưu trữ.");

        if (quotaBytes <= 0)
            throw ErrorHelper.Internal("Hạn mức lưu trữ chưa được cấu hình.");

        _logger.LogInformation("Listing objects to measure bucket {Bucket}.", bucketName);

        long usedBytes = 0;
        long objectCount = 0;

        try
        {
            string? continuationToken = null;
            do
            {
                var response = await _s3Client.ListObjectsV2Async(new ListObjectsV2Request
                {
                    BucketName = bucketName,
                    ContinuationToken = continuationToken
                }, cancellationToken);

                foreach (var obj in response.S3Objects ?? [])
                {
                    var size = obj.Size ?? 0;
                    if (size < 0)
                        throw ErrorHelper.WithStatus(502, "Kho lưu trữ trả kích thước object không hợp lệ.");

                    usedBytes += size;
                    objectCount++;
                }

                continuationToken = response.IsTruncated == true ? response.NextContinuationToken : null;
            }
            while (continuationToken != null);
        }
        catch (AmazonS3Exception ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            _logger.LogWarning(ex, "Object storage bucket {Bucket} was not found.", bucketName);
            throw ErrorHelper.NotFound("Không tìm thấy bucket lưu trữ.");
        }
        catch (AmazonS3Exception ex)
        {
            _logger.LogError(ex, "Object storage list failed for bucket {Bucket}.", bucketName);
            throw ErrorHelper.WithStatus(502, "Không liệt kê được object trong bucket lưu trữ.");
        }

        var remainingBytes = usedBytes >= quotaBytes ? 0 : quotaBytes - usedBytes;
        var usedPercent = Math.Round(usedBytes * 100d / quotaBytes, 2, MidpointRounding.AwayFromZero);

        return new R2StorageUsageDto
        {
            BucketName = bucketName,
            PayloadBytes = usedBytes,
            MetadataBytes = 0,
            UsedBytes = usedBytes,
            ObjectCount = objectCount,
            QuotaBytes = quotaBytes,
            RemainingBytes = remainingBytes,
            UsedPercent = usedPercent,
            MeasuredAt = DateTime.UtcNow
        };
    }
}
