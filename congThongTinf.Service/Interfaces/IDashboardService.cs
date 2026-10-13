using congThongTinf.Service.DTOs;

namespace congThongTinf.Service.Interfaces;

public interface IDashboardService
{
    Task<DashboardUserStatsDto> GetUsersAsync(DashboardQueryDto? query);

    Task<DashboardNewsSectionDto> GetNewsAsync(DashboardQueryDto? query);

    Task<DashboardDocumentStatsDto> GetDocumentsAsync(DashboardQueryDto? query);

    Task<DashboardCategoryStatsDto> GetCategoriesAsync();

    Task<DashboardBannerStatsDto> GetBannersAsync();
}
