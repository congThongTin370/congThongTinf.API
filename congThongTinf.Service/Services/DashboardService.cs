using congThongTinf.Service.DTOs;
using congThongTinf.Service.Interfaces;
using congThongTinf.Service.Interfaces.Commons;
using congThongTinf.Service.Utils;

namespace congThongTinf.Service.Services;

public class DashboardService : IDashboardService
{
    private const int PreviewTake = 5;

    private readonly IAdminService _adminService;
    private readonly ICategoryService _categoryService;
    private readonly IDocumentService _documentService;
    private readonly ILoggerService _logger;
    private readonly INewsService _newsService;
    private readonly IUserService _userService;

    public DashboardService(
        ILoggerService logger,
        IUserService userService,
        INewsService newsService,
        IDocumentService documentService,
        ICategoryService categoryService,
        IAdminService adminService)
    {
        _logger = logger;
        _userService = userService;
        _newsService = newsService;
        _documentService = documentService;
        _categoryService = categoryService;
        _adminService = adminService;
    }

    public async Task<DashboardUserStatsDto> GetUsersAsync(DashboardQueryDto? query)
    {
        var (from, toExclusive) = ResolveRange(query);
        _logger.Info($"[GetDashboardUsers] from={from:yyyy-MM-dd}, toExclusive={toExclusive:yyyy-MM-dd}");

        var result = await _userService.GetDashboardSnapshotAsync(from, toExclusive);

        _logger.Success("[GetDashboardUsers] Users snapshot assembled.");
        return result;
    }

    public async Task<DashboardNewsSectionDto> GetNewsAsync(DashboardQueryDto? query)
    {
        var (from, toExclusive) = ResolveRange(query);
        _logger.Info($"[GetDashboardNews] from={from:yyyy-MM-dd}, toExclusive={toExclusive:yyyy-MM-dd}");

        var news = await _newsService.GetDashboardSnapshotAsync(from, toExclusive, PreviewTake);

        var result = new DashboardNewsSectionDto
        {
            FromDate = from,
            ToDate = toExclusive.AddDays(-1),
            Stats = new DashboardNewsStatsDto
            {
                Total = news.Total,
                Draft = news.Draft,
                Pending = news.Pending,
                Published = news.Published,
                Archived = news.Archived,
                CreatedInRange = news.CreatedInRange,
                PublishedInRange = news.PublishedInRange,
                TotalViews = news.TotalViews
            },
            NewsByCategory = news.ByCategory,
            NewsByDay = news.ByDay,
            PendingNews = news.PendingItems,
            TopViewedNews = news.TopViewedItems
        };

        _logger.Success("[GetDashboardNews] News snapshot assembled.");
        return result;
    }

    public async Task<DashboardDocumentStatsDto> GetDocumentsAsync(DashboardQueryDto? query)
    {
        var (from, toExclusive) = ResolveRange(query);
        _logger.Info($"[GetDashboardDocuments] from={from:yyyy-MM-dd}, toExclusive={toExclusive:yyyy-MM-dd}");

        var documents = await _documentService.GetDashboardSnapshotAsync(from, toExclusive, PreviewTake);

        _logger.Success("[GetDashboardDocuments] Documents snapshot assembled.");
        return new DashboardDocumentStatsDto
        {
            Total = documents.Total,
            UploadedInRange = documents.UploadedInRange,
            TotalFileSize = documents.TotalFileSize,
            ByCategory = documents.ByCategory,
            RecentItems = documents.RecentItems
        };
    }

    public async Task<DashboardCategoryStatsDto> GetCategoriesAsync()
    {
        _logger.Info("[GetDashboardCategories] Requested.");
        var result = await _categoryService.GetDashboardSnapshotAsync();
        _logger.Success("[GetDashboardCategories] Categories snapshot assembled.");
        return result;
    }

    public async Task<DashboardBannerStatsDto> GetBannersAsync()
    {
        _logger.Info("[GetDashboardBanners] Requested.");
        var result = await _adminService.GetBannerDashboardSnapshotAsync();
        _logger.Success("[GetDashboardBanners] Banners snapshot assembled.");
        return result;
    }

    private static (DateTime From, DateTime ToExclusive) ResolveRange(DashboardQueryDto? query)
    {
        query ??= new DashboardQueryDto();

        if (query.FromDate.HasValue && query.ToDate.HasValue
            && query.FromDate.Value.Date > query.ToDate.Value.Date)
            throw ErrorHelper.BadRequest("Ngày bắt đầu phải nhỏ hơn hoặc bằng ngày kết thúc.");

        var from = query.FromDate?.Date ?? DateTime.UtcNow.Date.AddDays(-29);
        var toExclusive = (query.ToDate?.Date ?? DateTime.UtcNow.Date).AddDays(1);

        // Npgsql yêu cầu DateTime cho timestamptz phải có Kind=Utc; Unspecified sẽ gây lỗi khi so sánh.
        from = DateTime.SpecifyKind(from, DateTimeKind.Utc);
        toExclusive = DateTime.SpecifyKind(toExclusive, DateTimeKind.Utc);

        return (from, toExclusive);
    }
}
