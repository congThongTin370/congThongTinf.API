using congThongTinf.Service.DTOs;

namespace congThongTinf.Service.Interfaces;

public interface INewsService
{
    Task<List<NewsDto>> GetAllNewsAsync(NewsQueryDto? query);
}
