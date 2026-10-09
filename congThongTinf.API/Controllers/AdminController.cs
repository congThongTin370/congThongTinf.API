using congThongTinf.Infrastructure.Interfaces;
using congThongTinf.Service.DTOs;
using congThongTinf.Service.Interfaces;
using congThongTinf.Service.Utils;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace congThongTinf.API.Controllers;

[Route("api/admin")]
[ApiController]
[Authorize(Roles = "Admin")]
public class AdminController : ControllerBase
{
    private readonly IAdminService _adminService;
    private readonly IClaimsService _claimsService;

    public AdminController(IAdminService adminService, IClaimsService claimsService)
    {
        _adminService = adminService;
        _claimsService = claimsService;
    }

    [HttpPost("banners")]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(ApiResult<BannerDto>), 200)]
    [ProducesResponseType(typeof(ApiResult<BannerDto>), 400)]
    public async Task<IActionResult> UploadBanner([FromForm] UploadBannerForm form)
    {
        try
        {
            var result = await _adminService.UploadBannerAsync(
                _claimsService.CurrentUserId,
                form,
                HttpContext.RequestAborted);
            return Ok(ApiResult<BannerDto>.Success(result, "200", "Tải banner thành công."));
        }
        catch (Exception ex)
        {
            var statusCode = ExceptionUtils.ExtractStatusCode(ex);
            var errorResponse = ExceptionUtils.CreateErrorResponse<BannerDto>(ex);
            return StatusCode(statusCode, errorResponse);
        }
    }
}
