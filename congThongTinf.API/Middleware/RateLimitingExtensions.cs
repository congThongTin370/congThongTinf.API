using System.Threading.RateLimiting;
using congThongTinf.Service.Utils;
using Microsoft.AspNetCore.RateLimiting;

namespace congThongTinf.API.Middleware;

public static class RateLimitingExtensions
{
    public static IServiceCollection AddApiRateLimiting(this IServiceCollection services, IConfiguration configuration)
    {
        var settings = configuration.GetSection(RateLimitingOptions.SectionName).Get<RateLimitingOptions>()
                       ?? new RateLimitingOptions();

        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            options.OnRejected = WriteTooManyRequestsResponse;

            options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
            {
                if (!httpContext.Request.Path.StartsWithSegments("/api"))
                    return RateLimitPartition.GetNoLimiter("non-api");

                var partitionKey = GetClientKey(httpContext);
                return RateLimitPartition.GetSlidingWindowLimiter(partitionKey, _ => new SlidingWindowRateLimiterOptions
                {
                    PermitLimit = settings.PermitLimit,
                    Window = TimeSpan.FromSeconds(settings.WindowSeconds),
                    SegmentsPerWindow = 6,
                    QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                    QueueLimit = 0
                });
            });

            options.AddPolicy(RateLimitingOptions.LoginPolicy, httpContext =>
            {
                var partitionKey = $"login:{GetClientKey(httpContext)}";
                return RateLimitPartition.GetSlidingWindowLimiter(partitionKey, _ => new SlidingWindowRateLimiterOptions
                {
                    PermitLimit = settings.LoginPermitLimit,
                    Window = TimeSpan.FromSeconds(settings.LoginWindowSeconds),
                    SegmentsPerWindow = 6,
                    QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                    QueueLimit = 0
                });
            });
        });

        return services;
    }

    private static string GetClientKey(HttpContext httpContext)
    {
        return httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
    }

    private static async ValueTask WriteTooManyRequestsResponse(OnRejectedContext context, CancellationToken cancellationToken)
    {
        var response = context.HttpContext.Response;
        response.StatusCode = StatusCodes.Status429TooManyRequests;

        if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
            response.Headers.RetryAfter = ((int)Math.Ceiling(retryAfter.TotalSeconds)).ToString();

        var body = ApiResult.Failure("429", "Quá nhiều yêu cầu. Vui lòng thử lại sau.");
        await response.WriteAsJsonAsync(body, cancellationToken);
    }
}
