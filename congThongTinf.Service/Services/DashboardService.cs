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

    public async Task<DashboardDto> GetDashboardAsync(DashboardQueryDto? query)
    {
        query ??= new DashboardQueryDto();

        if (query.FromDate.HasValue && query.ToDate.HasValue
            && query.FromDate.Value.Date > query.ToDate.Value.Date)
            throw ErrorHelper.BadRequest("Ngày bắt đầu phải nhỏ hơn hoặc bằng ngày kết thúc.");

        var from = (query.FromDate?.Date ?? DateTime.UtcNow.Date.AddDays(-29));
        var toExclusive = (query.ToDate?.Date ?? DateTime.UtcNow.Date).AddDays(1);

        _logger.Info($"[GetDashboard] from={from:yyyy-MM-dd}, toExclusive={toExclusive:yyyy-MM-dd}");

        var usersTask = _userService.GetDashboardSnapshotAsync(from, toExclusive);
        var newsTask = _newsService.GetDashboardSnapshotAsync(from, toExclusive, PreviewTake);
        var documentsTask = _documentService.GetDashboardSnapshotAsync(from, toExclusive, PreviewTake);
        var categoriesTask = _categoryService.GetDashboardSnapshotAsync();
        var bannersTask = _adminService.GetBannerDashboardSnapshotAsync();

        await Task.WhenAll(usersTask, newsTask, documentsTask, categoriesTask, bannersTask);

        var news = await newsTask;
        var documents = await documentsTask;

        var result = new DashboardDto
        {
            FromDate = from,
            ToDate = toExclusive.AddDays(-1),
            Users = await usersTask,
            News = new DashboardNewsStatsDto
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
            Documents = new DashboardDocumentStatsDto
            {
                Total = documents.Total,
                UploadedInRange = documents.UploadedInRange,
                TotalFileSize = documents.TotalFileSize,
                ByCategory = documents.ByCategory
            },
            Categories = await categoriesTask,
            Banners = await bannersTask,
            NewsByCategory = news.ByCategory,
            NewsByDay = news.ByDay,
            PendingNews = news.PendingItems,
            TopViewedNews = news.TopViewedItems,
            RecentDocuments = documents.RecentItems
        };

        _logger.Success("[GetDashboard] Dashboard snapshot assembled.");
        return result;
    }
}
