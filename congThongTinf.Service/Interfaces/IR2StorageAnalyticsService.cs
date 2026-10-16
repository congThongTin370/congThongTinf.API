using congThongTinf.Service.DTOs;

namespace congThongTinf.Service.Interfaces;

public interface IR2StorageAnalyticsService
{
    Task<R2StorageUsageDto> GetUsageAsync(CancellationToken cancellationToken = default);
}
