using congThongTinf.Domain.Entities;
using congThongTinf.Infrastructure.Commons;
using congThongTinf.Service.DTOs;
using congThongTinf.Service.Interfaces;
using congThongTinf.Service.Interfaces.Commons;
using congThongTinf.Service.Utils;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace congThongTinf.Service.Services;

public class NewsService : INewsService
{
    private static readonly HashSet<string> ImageExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".jpg", ".jpeg", ".png", ".webp", ".gif"
    };

    private readonly ILoggerService _logger;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IAdminService _adminService;
    private readonly IBlobService _blobService;

    public NewsService(
        ILoggerService logger,
        IUnitOfWork unitOfWork,
        IAdminService adminService,
        IBlobService blobService)
    {
        _logger = logger;
        _unitOfWork = unitOfWork;
        _adminService = adminService;
        _blobService = blobService;
    }

    public async Task<Pagination<NewsDto>> GetAllNewsAsync(NewsQueryDto? query)
    {
        query ??= new NewsQueryDto();
        if (query.PageSize <= 0)
            query.PageSize = 10;

        if (query.FromDate.HasValue && query.ToDate.HasValue
            && query.FromDate.Value.Date > query.ToDate.Value.Date)
            throw ErrorHelper.BadRequest("Ngày bắt đầu phải nhỏ hơn hoặc bằng ngày kết thúc.");

        DateTime? from = query.FromDate.HasValue
            ? DateTimeUtc.StartOfDay(query.FromDate.Value)
            : null;
        DateTime? toExclusive = query.ToDate.HasValue
            ? DateTimeUtc.ExclusiveEndOfDay(query.ToDate.Value)
            : null;
        var categoryIds = query.CategoryIds?
            .Where(id => id != Guid.Empty)
            .Distinct()
            .ToList();

        _logger.Info(
            $"[GetAllNews] page={query.PageIndex}, size={query.PageSize}, from={from:yyyy-MM-dd}, to={query.ToDate:yyyy-MM-dd}, categories={categoryIds?.Count ?? 0}");

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

        articles = articles.OrderByDescending(n => n.CreatedAt);

        var total = await articles.CountAsync();

        List<News> result;
        if (query.PageIndex == 0)
            result = await articles.ToListAsync();
        else
            result = await articles
                .Skip((query.PageIndex - 1) * query.PageSize)
                .Take(query.PageSize)
                .ToListAsync();

        _logger.Success($"[GetAllNews] Returned {result.Count} of {total} articles.");
        return new Pagination<NewsDto>(result.Select(ToDto).ToList(), total, query.PageIndex, query.PageSize);
    }

    public async Task<DashboardNewsStatsDto> GetDashboardSnapshotAsync(DateTime from, DateTime toExclusive, int previewTake)
    {
        if (previewTake <= 0)
            previewTake = 5;

        from = DateTimeUtc.AsUtc(from);
        toExclusive = DateTimeUtc.AsUtc(toExclusive);

        var news = _unitOfWork.News.GetQueryable().AsNoTracking();

        var statusCounts = await news
            .GroupBy(n => n.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToListAsync();

        int CountStatus(NewsStatus status) =>
            statusCounts.FirstOrDefault(s => s.Status == status)?.Count ?? 0;

        var byCategory = await news
            .GroupBy(n => new { n.CategoryId, Name = n.Category != null ? n.Category.Name : string.Empty })
            .Select(g => new DashboardCategoryCountDto
            {
                CategoryId = g.Key.CategoryId,
                CategoryName = g.Key.Name,
                Count = g.Count()
            })
            .OrderByDescending(x => x.Count)
            .ToListAsync();

        var createdDates = await news
            .Where(n => n.CreatedAt >= from && n.CreatedAt < toExclusive)
            .Select(n => n.CreatedAt)
            .ToListAsync();

        var publishedDates = await news
            .Where(n => n.PublishedAt.HasValue && n.PublishedAt.Value >= from && n.PublishedAt.Value < toExclusive)
            .Select(n => n.PublishedAt!.Value)
            .ToListAsync();

        var createdByDay = createdDates.GroupBy(d => d.Date).ToDictionary(g => g.Key, g => g.Count());
        var publishedByDay = publishedDates.GroupBy(d => d.Date).ToDictionary(g => g.Key, g => g.Count());

        var byDay = new List<DashboardCountByDayDto>();
        for (var day = from.Date; day < toExclusive; day = day.AddDays(1))
        {
            byDay.Add(new DashboardCountByDayDto
            {
                Date = day,
                Created = createdByDay.GetValueOrDefault(day),
                Published = publishedByDay.GetValueOrDefault(day)
            });
        }

        var pendingItems = await news
            .Where(n => n.Status == NewsStatus.Pending)
            .OrderByDescending(n => n.CreatedAt)
            .Take(previewTake)
            .Select(n => new DashboardNewsPreviewDto
            {
                Id = n.Id,
                Title = n.Title,
                AuthorName = n.Author != null ? n.Author.FullName : string.Empty,
                CategoryName = n.Category != null ? n.Category.Name : string.Empty,
                ViewCount = n.ViewCount,
                CreatedAt = n.CreatedAt
            })
            .ToListAsync();

        var topViewedItems = await news
            .Where(n => n.Status == NewsStatus.Published)
            .OrderByDescending(n => n.ViewCount)
            .ThenByDescending(n => n.PublishedAt)
            .Take(previewTake)
            .Select(n => new DashboardNewsPreviewDto
            {
                Id = n.Id,
                Title = n.Title,
                AuthorName = n.Author != null ? n.Author.FullName : string.Empty,
                CategoryName = n.Category != null ? n.Category.Name : string.Empty,
                ViewCount = n.ViewCount,
                CreatedAt = n.CreatedAt
            })
            .ToListAsync();

        return new DashboardNewsStatsDto
        {
            Total = statusCounts.Sum(s => s.Count),
            Draft = CountStatus(NewsStatus.Draft),
            Pending = CountStatus(NewsStatus.Pending),
            Published = CountStatus(NewsStatus.Published),
            Archived = CountStatus(NewsStatus.Archived),
            CreatedInRange = createdDates.Count,
            PublishedInRange = publishedDates.Count,
            TotalViews = await news.SumAsync(n => (int?)n.ViewCount) ?? 0,
            ByCategory = byCategory,
            ByDay = byDay,
            PendingItems = pendingItems,
            TopViewedItems = topViewedItems
        };
    }

    public async Task<NewsDto> GetNewsByIdAsync(Guid id)
    {
        _logger.Info($"[GetNewsById] Requests news {id}");

        var news = await GetNewsWithRelationsAsync(id);
        if (news == null)
        {
            _logger.Warn($"[GetNewsById] News {id} not found.");
            throw ErrorHelper.NotFound($"Bài báo với ID {id} không tồn tại.");
        }

        return ToDto(news);
    }

    public async Task<int> IncrementViewCountAsync(Guid id)
    {
        _logger.Info($"[IncrementViewCount] News {id}");

        var news = await _unitOfWork.News.GetByIdAsync(id);
        if (news == null)
        {
            _logger.Warn($"[IncrementViewCount] News {id} not found.");
            throw ErrorHelper.NotFound($"Bài báo với ID {id} không tồn tại.");
        }

        news.ViewCount++;
        await _unitOfWork.SaveChangesAsync();

        _logger.Success($"[IncrementViewCount] News {id} viewCount={news.ViewCount}.");
        return news.ViewCount;
    }

    public async Task<NewsDto> CreateNewsAsync(Guid currentUserId, CreateNewsDto dto)
    {
        _logger.Info($"[CreateNews] User {currentUserId} starts creating news.");

        await EnsureJournalistAsync(currentUserId);

        if (string.IsNullOrWhiteSpace(dto.Title)
            || string.IsNullOrWhiteSpace(dto.Content)
            || dto.CategoryId == Guid.Empty)
            throw ErrorHelper.BadRequest("Tiêu đề, nội dung và danh mục là bắt buộc.");

        var category = await _unitOfWork.Categories.GetByIdAsync(dto.CategoryId);
        if (category == null)
            throw ErrorHelper.NotFound($"Danh mục với ID {dto.CategoryId} không tồn tại.");

        // Ảnh bìa là tùy chọn. Nếu có, upload lên bucket và lấy URL để lưu xuống DB.
        string? coverImageUrl = null;
        if (dto.CoverImage != null)
        {
            ValidateImage(dto.CoverImage);

            var upload = await _blobService.UploadFileAsync(dto.CoverImage, $"news/{currentUserId}");
            coverImageUrl = upload.Url;
        }

        var news = new News
        {
            Title = dto.Title.Trim(),
            Summary = dto.Summary?.Trim() ?? string.Empty,
            Content = dto.Content,
            CoverImageUrl = coverImageUrl,
            CategoryId = dto.CategoryId,
            AuthorId = currentUserId,
            Status = NewsStatus.Draft,
        };

        await _unitOfWork.News.AddAsync(news);
        await _unitOfWork.SaveChangesAsync();

        _logger.Success($"[CreateNews] News {news.Id} created by user {currentUserId}.");
        return await GetNewsByIdAsync(news.Id);
    }

    public async Task<NewsDto> UpdateNewsAsync(Guid currentUserId, Guid id, UpdateNewsDto dto)
    {
        _logger.Info($"[UpdateNews] User {currentUserId} starts updating news {id}.");

        await EnsureJournalistAsync(currentUserId);

        var news = await _unitOfWork.News.GetByIdAsync(id);
        if (news == null)
        {
            _logger.Warn($"[UpdateNews] News {id} not found.");
            throw ErrorHelper.NotFound($"Bài báo với ID {id} không tồn tại.");
        }

        EnsureOwner(news, currentUserId);

        if (string.IsNullOrWhiteSpace(dto.Title)
            || string.IsNullOrWhiteSpace(dto.Content)
            || dto.CategoryId == Guid.Empty)
            throw ErrorHelper.BadRequest("Tiêu đề, nội dung và danh mục là bắt buộc.");

        var category = await _unitOfWork.Categories.GetByIdAsync(dto.CategoryId);
        if (category == null)
            throw ErrorHelper.NotFound($"Danh mục với ID {dto.CategoryId} không tồn tại.");

        news.Title = dto.Title.Trim();
        news.Summary = dto.Summary?.Trim() ?? string.Empty;
        news.Content = dto.Content;
        news.CategoryId = dto.CategoryId;

        if (dto.CoverImage != null)
        {
            ValidateImage(dto.CoverImage);

            var upload = await _blobService.UploadFileAsync(dto.CoverImage, $"news/{currentUserId}");
            var previousCoverUrl = news.CoverImageUrl;
            news.CoverImageUrl = upload.Url;
            await TryDeleteCoverBlobAsync(previousCoverUrl);
        }

        await _unitOfWork.News.Update(news);
        await _unitOfWork.SaveChangesAsync();

        _logger.Success($"[UpdateNews] News {id} updated by user {currentUserId}.");
        return await GetNewsByIdAsync(id);
    }

    public async Task<bool> DeleteNewsAsync(Guid currentUserId, Guid id)
    {
        _logger.Info($"[DeleteNews] User {currentUserId} starts deleting news {id}.");

        await EnsureJournalistAsync(currentUserId);

        var news = await _unitOfWork.News.GetByIdAsync(id);
        if (news == null)
        {
            _logger.Warn($"[DeleteNews] News {id} not found.");
            throw ErrorHelper.NotFound($"Bài báo với ID {id} không tồn tại.");
        }

        EnsureOwner(news, currentUserId);

        await _unitOfWork.News.HardRemoveRange(new List<News> { news });
        await _unitOfWork.SaveChangesAsync();

        _logger.Success($"[DeleteNews] News {id} deleted by user {currentUserId}.");
        return true;
    }

    public async Task<NewsImageDto> UploadImageAsync(
        Guid currentUserId,
        IFormFile? file,
        CancellationToken cancellationToken = default)
    {
        _logger.Info($"[UploadImage] User {currentUserId} uploads a news image.");

        await EnsureJournalistAsync(currentUserId);

        ValidateImage(file);

        // news/{userId}/{guid}.ext — the user folder is the ownership trail; no image table.
        var upload = await _blobService.UploadFileAsync(file, $"news/{currentUserId}", cancellationToken);

        _logger.Success($"[UploadImage] Stored {upload.Key} for user {currentUserId}.");
        return new NewsImageDto { Url = upload.Url };
    }

    private async Task TryDeleteCoverBlobAsync(string? coverImageUrl)
    {
        if (string.IsNullOrWhiteSpace(coverImageUrl))
            return;

        try
        {
            var key = _blobService.GetKeyFromPublicUrl(coverImageUrl);
            await _blobService.DeleteFileAsync(key);
        }
        catch (Exception ex)
        {
            _logger.Warn($"[TryDeleteCoverBlob] Failed to delete previous cover '{coverImageUrl}': {ex.Message}");
        }
    }

    /// <summary>
    ///     Throws 400 unless the file is non-empty and has an allowed image extension.
    /// </summary>
    private static void ValidateImage(IFormFile? file)
    {
        if (file == null || file.Length == 0)
            throw ErrorHelper.BadRequest("File là bắt buộc.");

        var extension = Path.GetExtension(file.FileName);
        if (string.IsNullOrWhiteSpace(extension) || !ImageExtensions.Contains(extension))
            throw ErrorHelper.BadRequest("Ảnh phải là jpg, jpeg, png, webp hoặc gif.");
    }

    /// <summary>
    ///     Throws 403 unless the current user exists, is active, and has the Journalist role.
    /// </summary>
    private async Task EnsureJournalistAsync(Guid currentUserId)
    {
        var user = await _adminService.GetUserById(currentUserId);
        if (user == null)
            throw ErrorHelper.Unauthorized("Không xác định được người dùng hiện tại.");

        var role = await _unitOfWork.Roles.GetByIdAsync(user.RoleId);
        if (role?.Name != RoleType.Journalist)
            throw ErrorHelper.Forbidden("Chỉ phóng viên mới được thực hiện thao tác này.");
    }

    /// <summary>
    ///     Throws 403 unless the current user is the author of the article.
    /// </summary>
    private static void EnsureOwner(News news, Guid currentUserId)
    {
        if (news.AuthorId != currentUserId)
            throw ErrorHelper.Forbidden("Bạn chỉ được thao tác trên bài viết của chính mình.");
    }

    private async Task<News?> GetNewsWithRelationsAsync(Guid id)
    {
        return await _unitOfWork.News.GetQueryable()
            .Include(n => n.Category)
            .Include(n => n.Author)
            .FirstOrDefaultAsync(n => n.Id == id);
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
