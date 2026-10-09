using congThongTinf.Service.DTOs;

namespace congThongTinf.Service.Interfaces;

public interface INewsService
{
    Task<List<NewsDto>> GetAllNewsAsync(NewsQueryDto? query);

    Task<NewsDto> GetNewsByIdAsync(Guid id);

    Task<NewsDto> CreateNewsAsync(Guid currentUserId, CreateNewsDto dto);

    Task<NewsDto> UpdateNewsAsync(Guid currentUserId, Guid id, UpdateNewsDto dto);

    Task<bool> DeleteNewsAsync(Guid currentUserId, Guid id);
}
