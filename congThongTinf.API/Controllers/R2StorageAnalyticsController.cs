using congThongTinf.Service.DTOs;
using congThongTinf.Service.Interfaces;
using congThongTinf.Service.Utils;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace congThongTinf.API.Controllers;

[Route("api/admin/storage")]
[ApiController]
[Authorize(Roles = "Admin")]
public class R2StorageAnalyticsController : ControllerBase
{
    private readonly IR2StorageAnalyticsService _storageAnalyticsService;

    public R2StorageAnalyticsController(IR2StorageAnalyticsService storageAnalyticsService)
    {
        _storageAnalyticsService = storageAnalyticsService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(ApiResult<R2StorageUsageDto>), 200)]
    [ProducesResponseType(typeof(ApiResult<R2StorageUsageDto>), 404)]
    [ProducesResponseType(typeof(ApiResult<R2StorageUsageDto>), 500)]
    [ProducesResponseType(typeof(ApiResult<R2StorageUsageDto>), 502)]
    public async Task<IActionResult> GetUsage(CancellationToken cancellationToken)
    {
        try
        {
            var result = await _storageAnalyticsService.GetUsageAsync(cancellationToken);
            return Ok(ApiResult<R2StorageUsageDto>.Success(result, "200", "Lấy dung lượng R2 thành công."));
        }
        catch (Exception ex)
        {
            var statusCode = ExceptionUtils.ExtractStatusCode(ex);
            var errorResponse = ExceptionUtils.CreateErrorResponse<R2StorageUsageDto>(ex);
            return StatusCode(statusCode, errorResponse);
        }
    }
}
