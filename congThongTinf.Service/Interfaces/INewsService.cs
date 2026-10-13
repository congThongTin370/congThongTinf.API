using congThongTinf.Infrastructure.Commons;
using congThongTinf.Service.DTOs;
using Microsoft.AspNetCore.Http;

namespace congThongTinf.Service.Interfaces;

public interface INewsService
{
    Task<Pagination<NewsDto>> GetAllNewsAsync(NewsQueryDto? query);

    Task<NewsDto> GetNewsByIdAsync(Guid id);

    Task<int> IncrementViewCountAsync(Guid id);

    Task<NewsDto> CreateNewsAsync(Guid currentUserId, CreateNewsDto dto);

    Task<NewsDto> UpdateNewsAsync(Guid currentUserId, Guid id, UpdateNewsDto dto);

    Task<bool> DeleteNewsAsync(Guid currentUserId, Guid id);

    Task<NewsImageDto> UploadImageAsync(Guid currentUserId, IFormFile? file, CancellationToken cancellationToken = default);

    Task<DashboardNewsStatsDto> GetDashboardSnapshotAsync(DateTime from, DateTime toExclusive, int previewTake);
}
