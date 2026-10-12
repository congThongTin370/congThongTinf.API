using congThongTinf.Service.DTOs;

namespace congThongTinf.Service.Interfaces;

public interface IDashboardService
{
    Task<DashboardDto> GetDashboardAsync(DashboardQueryDto? query);
}
