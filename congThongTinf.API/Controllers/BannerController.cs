using congThongTinf.Infrastructure.Commons;
using congThongTinf.Infrastructure.Interfaces;
using congThongTinf.Service.DTOs;
using congThongTinf.Service.Interfaces;
using congThongTinf.Service.Utils;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace congThongTinf.API.Controllers;

[Route("api/admin/banners")]
[ApiController]
public class BannerController : ControllerBase
{
    private readonly IBannerService _bannerService;
    private readonly IClaimsService _claimsService;

    public BannerController(IBannerService bannerService, IClaimsService claimsService)
    {
        _bannerService = bannerService;
        _claimsService = claimsService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(ApiResult<Pagination<BannerDto>>), 200)]
    public async Task<IActionResult> GetAll([FromQuery] BannerQueryParameter param)
    {
        try
        {
            var result = await _bannerService.GetAllBannersAsync(param);
            return Ok(ApiResult<Pagination<BannerDto>>.Success(result, "200", "Lấy danh sách banner thành công."));
        }
        catch (Exception ex)
        {
            var statusCode = ExceptionUtils.ExtractStatusCode(ex);
            var errorResponse = ExceptionUtils.CreateErrorResponse<Pagination<BannerDto>>(ex);
            return StatusCode(statusCode, errorResponse);
        }
    }

    [HttpGet("files")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(ApiResult<List<BlobObjectDto>>), 200)]
    [ProducesResponseType(typeof(ApiResult<List<BlobObjectDto>>), 502)]
    public async Task<IActionResult> ListFiles()
    {
        try
        {
            var result = await _bannerService.ListBannerImagesAsync(HttpContext.RequestAborted);
            return Ok(ApiResult<List<BlobObjectDto>>.Success(result, "200", "Lấy danh sách ảnh banner thành công."));
        }
        catch (Exception ex)
        {
            var statusCode = ExceptionUtils.ExtractStatusCode(ex);
            var errorResponse = ExceptionUtils.CreateErrorResponse<List<BlobObjectDto>>(ex);
            return StatusCode(statusCode, errorResponse);
        }
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ApiResult<BannerDto>), 200)]
    [ProducesResponseType(typeof(ApiResult<BannerDto>), 404)]
    public async Task<IActionResult> GetById(Guid id)
    {
        try
        {
            var result = await _bannerService.GetBannerByIdAsync(id);
            return Ok(ApiResult<BannerDto>.Success(result, "200", "Lấy banner thành công."));
        }
        catch (Exception ex)
        {
            var statusCode = ExceptionUtils.ExtractStatusCode(ex);
            var errorResponse = ExceptionUtils.CreateErrorResponse<BannerDto>(ex);
            return StatusCode(statusCode, errorResponse);
        }
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(ApiResult<BannerDto>), 200)]
    [ProducesResponseType(typeof(ApiResult<BannerDto>), 400)]
    public async Task<IActionResult> Upload([FromForm] UploadBannerForm form)
    {
        try
        {
            var result = await _bannerService.UploadBannerAsync(
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

    [HttpPut("{id:guid}")]
    [Consumes("multipart/form-data")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(ApiResult<BannerDto>), 200)]
    [ProducesResponseType(typeof(ApiResult<BannerDto>), 400)]
    [ProducesResponseType(typeof(ApiResult<BannerDto>), 404)]
    public async Task<IActionResult> Update(Guid id, [FromForm] UpdateBannerForm form)
    {
        try
        {
            var result = await _bannerService.UpdateBannerAsync(id, form, HttpContext.RequestAborted);
            return Ok(ApiResult<BannerDto>.Success(result, "200", "Cập nhật banner thành công."));
        }
        catch (Exception ex)
        {
            var statusCode = ExceptionUtils.ExtractStatusCode(ex);
            var errorResponse = ExceptionUtils.CreateErrorResponse<BannerDto>(ex);
            return StatusCode(statusCode, errorResponse);
        }
    }

    [HttpPut("{id:guid}/active")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(ApiResult<BannerDto>), 200)]
    [ProducesResponseType(typeof(ApiResult<BannerDto>), 404)]
    public async Task<IActionResult> SetBannerActive(Guid id)
    {
        try
        {
            var result = await _bannerService.SetBannerActiveAsync(id);
            return Ok(ApiResult<BannerDto>.Success(result, "200", "Đặt banner hiển thị cho vị trí thành công."));
        }
        catch (Exception ex)
        {
            var statusCode = ExceptionUtils.ExtractStatusCode(ex);
            var errorResponse = ExceptionUtils.CreateErrorResponse<BannerDto>(ex);
            return StatusCode(statusCode, errorResponse);
        }
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Admin")]    
    [ProducesResponseType(typeof(ApiResult<bool>), 200)]
    [ProducesResponseType(typeof(ApiResult<bool>), 404)]
    public async Task<IActionResult> Delete(Guid id)
    {
        try
        {
            var result = await _bannerService.DeleteBannerAsync(id, HttpContext.RequestAborted);
            return Ok(ApiResult<bool>.Success(result, "200", "Xóa banner thành công."));
        }
        catch (Exception ex)
        {
            var statusCode = ExceptionUtils.ExtractStatusCode(ex);
            var errorResponse = ExceptionUtils.CreateErrorResponse<bool>(ex);
            return StatusCode(statusCode, errorResponse);
        }
    }
}
