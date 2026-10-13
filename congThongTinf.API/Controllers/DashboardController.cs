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

    [HttpGet("users")]
    [ProducesResponseType(typeof(ApiResult<DashboardUserStatsDto>), 200)]
    [ProducesResponseType(typeof(ApiResult<DashboardUserStatsDto>), 400)]
    public async Task<IActionResult> GetUsers([FromQuery] DashboardQueryDto query)
    {
        try
        {
            var result = await _dashboardService.GetUsersAsync(query);
            return Ok(ApiResult<DashboardUserStatsDto>.Success(result, "200", "Lấy thống kê người dùng thành công."));
        }
        catch (Exception ex)
        {
            var statusCode = ExceptionUtils.ExtractStatusCode(ex);
            var errorResponse = ExceptionUtils.CreateErrorResponse<DashboardUserStatsDto>(ex);
            return StatusCode(statusCode, errorResponse);
        }
    }

    [HttpGet("news")]
    [ProducesResponseType(typeof(ApiResult<DashboardNewsSectionDto>), 200)]
    [ProducesResponseType(typeof(ApiResult<DashboardNewsSectionDto>), 400)]
    public async Task<IActionResult> GetNews([FromQuery] DashboardQueryDto query)
    {
        try
        {
            var result = await _dashboardService.GetNewsAsync(query);
            return Ok(ApiResult<DashboardNewsSectionDto>.Success(result, "200", "Lấy thống kê tin tức thành công."));
        }
        catch (Exception ex)
        {
            var statusCode = ExceptionUtils.ExtractStatusCode(ex);
            var errorResponse = ExceptionUtils.CreateErrorResponse<DashboardNewsSectionDto>(ex);
            return StatusCode(statusCode, errorResponse);
        }
    }

    [HttpGet("documents")]
    [ProducesResponseType(typeof(ApiResult<DashboardDocumentStatsDto>), 200)]
    [ProducesResponseType(typeof(ApiResult<DashboardDocumentStatsDto>), 400)]
    public async Task<IActionResult> GetDocuments([FromQuery] DashboardQueryDto query)
    {
        try
        {
            var result = await _dashboardService.GetDocumentsAsync(query);
            return Ok(ApiResult<DashboardDocumentStatsDto>.Success(result, "200", "Lấy thống kê tài liệu thành công."));
        }
        catch (Exception ex)
        {
            var statusCode = ExceptionUtils.ExtractStatusCode(ex);
            var errorResponse = ExceptionUtils.CreateErrorResponse<DashboardDocumentStatsDto>(ex);
            return StatusCode(statusCode, errorResponse);
        }
    }

    [HttpGet("categories")]
    [ProducesResponseType(typeof(ApiResult<DashboardCategoryStatsDto>), 200)]
    public async Task<IActionResult> GetCategories()
    {
        try
        {
            var result = await _dashboardService.GetCategoriesAsync();
            return Ok(ApiResult<DashboardCategoryStatsDto>.Success(result, "200", "Lấy thống kê danh mục thành công."));
        }
        catch (Exception ex)
        {
            var statusCode = ExceptionUtils.ExtractStatusCode(ex);
            var errorResponse = ExceptionUtils.CreateErrorResponse<DashboardCategoryStatsDto>(ex);
            return StatusCode(statusCode, errorResponse);
        }
    }

    [HttpGet("banners")]
    [ProducesResponseType(typeof(ApiResult<DashboardBannerStatsDto>), 200)]
    public async Task<IActionResult> GetBanners()
    {
        try
        {
            var result = await _dashboardService.GetBannersAsync();
            return Ok(ApiResult<DashboardBannerStatsDto>.Success(result, "200", "Lấy thống kê banner thành công."));
        }
        catch (Exception ex)
        {
            var statusCode = ExceptionUtils.ExtractStatusCode(ex);
            var errorResponse = ExceptionUtils.CreateErrorResponse<DashboardBannerStatsDto>(ex);
            return StatusCode(statusCode, errorResponse);
        }
    }
}
