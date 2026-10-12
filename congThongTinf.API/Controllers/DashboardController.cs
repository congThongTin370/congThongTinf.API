using congThongTinf.Service.DTOs;
using congThongTinf.Service.Interfaces;
using congThongTinf.Service.Utils;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace congThongTinf.API.Controllers;

[Route("api/dashboard")]
[ApiController]
[Authorize(Roles = "Admin")]
public class DashboardController : ControllerBase
{
    private readonly IDashboardService _dashboardService;

    public DashboardController(IDashboardService dashboardService)
    {
        _dashboardService = dashboardService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(ApiResult<DashboardDto>), 200)]
    [ProducesResponseType(typeof(ApiResult<DashboardDto>), 400)]
    [ProducesResponseType(typeof(ApiResult<DashboardDto>), 403)]
    public async Task<IActionResult> Get([FromQuery] DashboardQueryDto query)
    {
        try
        {
            var result = await _dashboardService.GetDashboardAsync(query);
            return Ok(ApiResult<DashboardDto>.Success(result, "200", "Lấy dữ liệu dashboard thành công."));
        }
        catch (Exception ex)
        {
            var statusCode = ExceptionUtils.ExtractStatusCode(ex);
            var errorResponse = ExceptionUtils.CreateErrorResponse<DashboardDto>(ex);
            return StatusCode(statusCode, errorResponse);
        }
    }
}
