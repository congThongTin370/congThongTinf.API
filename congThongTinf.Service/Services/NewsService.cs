using congThongTinf.Domain.Entities;
using congThongTinf.Service.DTOs;
using congThongTinf.Service.Interfaces;
using congThongTinf.Service.Utils;
using Microsoft.EntityFrameworkCore;

namespace congThongTinf.Service.Services;

public class NewsService : INewsService
{
    private readonly ILoggerService _logger;
    private readonly IUnitOfWork _unitOfWork;

    public NewsService(ILoggerService logger, IUnitOfWork unitOfWork)
    {
        _logger = logger;
        _unitOfWork = unitOfWork;
    }

    public async Task<List<NewsDto>> GetAllNewsAsync(NewsQueryDto? query)
    {
        query ??= new NewsQueryDto();

        if (query.FromDate.HasValue && query.ToDate.HasValue
            && query.FromDate.Value.Date > query.ToDate.Value.Date)
            throw ErrorHelper.BadRequest("Ngày bắt đầu phải nhỏ hơn hoặc bằng ngày kết thúc.");

        var from = query.FromDate?.Date;
        DateTime? toExclusive = query.ToDate.HasValue
            ? query.ToDate.Value.Date.AddDays(1)
            : null;
        var categoryIds = query.CategoryIds?
            .Where(id => id != Guid.Empty)
            .Distinct()
            .ToList();

        _logger.Info(
            $"[GetAllNews] from={from:yyyy-MM-dd}, to={query.ToDate:yyyy-MM-dd}, categories={categoryIds?.Count ?? 0}");

        var articles = _unitOfWork.News.GetQueryable()
            .Include(n => n.Category)
            .Include(n => n.Author)
            .AsQueryable();

        if (from.HasValue)
            articles = articles.Where(n => n.CreatedAt >= from.Value);

        if (toExclusive.HasValue)
            articles = articles.Where(n => n.CreatedAt < toExclusive.Value);

        if (categoryIds is { Count: > 0 })
            articles = articles.Where(n => categoryIds.Contains(n.CategoryId));

        var result = await articles
            .OrderByDescending(n => n.CreatedAt)
            .ToListAsync();

        _logger.Success($"[GetAllNews] Returned {result.Count} articles.");
        return result.Select(ToDto).ToList();
    }

    private static NewsDto ToDto(News news)
    {
        return new NewsDto
        {
            Id = news.Id,
            Title = news.Title,
            Summary = news.Summary,
            Content = news.Content,
            CoverImageUrl = news.CoverImageUrl,
            CategoryId = news.CategoryId,
            CategoryName = news.Category?.Name ?? string.Empty,
            AuthorId = news.AuthorId,
            AuthorName = news.Author?.FullName ?? string.Empty,
            Status = news.Status,
            PublishedAt = news.PublishedAt,
            CreatedAt = news.CreatedAt,
            UpdatedAt = news.UpdatedAt,
            ViewCount = news.ViewCount
        };
    }
}
