using congThongTinf.Infrastructure.Commons;
using congThongTinf.Infrastructure.Interfaces;
using congThongTinf.Service.DTOs;
using congThongTinf.Service.Interfaces;
using congThongTinf.Service.Utils;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace congThongTinf.API.Controllers;

[Route("api/shared-files")]
[ApiController]
[Authorize]
public class SharedFileController : ControllerBase
{
    private readonly IClaimsService _claimsService;
    private readonly ISharedFileService _sharedFileService;

    public SharedFileController(ISharedFileService sharedFileService, IClaimsService claimsService)
    {
        _sharedFileService = sharedFileService;
        _claimsService = claimsService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(ApiResult<Pagination<SharedFileDto>>), 200)]
    public async Task<IActionResult> GetMine([FromQuery] SharedFileQueryParameter param)
    {
        try
        {
            var result = await _sharedFileService.GetMyFilesAsync(_claimsService.CurrentUserId, param);
            return Ok(ApiResult<Pagination<SharedFileDto>>.Success(result, "200", "Lấy danh sách file của bạn thành công."));
        }
        catch (Exception ex)
        {
            var statusCode = ExceptionUtils.ExtractStatusCode(ex);
            var errorResponse = ExceptionUtils.CreateErrorResponse<Pagination<SharedFileDto>>(ex);
            return StatusCode(statusCode, errorResponse);
        }
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ApiResult<SharedFileDto>), 200)]
    [ProducesResponseType(typeof(ApiResult<SharedFileDto>), 404)]
    public async Task<IActionResult> GetMineById(Guid id)
    {
        try
        {
            var result = await _sharedFileService.GetMyFileAsync(_claimsService.CurrentUserId, id);
            return Ok(ApiResult<SharedFileDto>.Success(result, "200", "Lấy thông tin file thành công."));
        }
        catch (Exception ex)
        {
            var statusCode = ExceptionUtils.ExtractStatusCode(ex);
            var errorResponse = ExceptionUtils.CreateErrorResponse<SharedFileDto>(ex);
            return StatusCode(statusCode, errorResponse);
        }
    }

    [HttpPost]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(ApiResult<SharedFileDto>), 200)]
    [ProducesResponseType(typeof(ApiResult<SharedFileDto>), 400)]
    public async Task<IActionResult> Upload([FromForm] UploadSharedFileForm form)
    {
        try
        {
            var result = await _sharedFileService.UploadAsync(
                _claimsService.CurrentUserId,
                form,
                HttpContext.RequestAborted);
            return Ok(ApiResult<SharedFileDto>.Success(result, "200", "Tải file chia sẻ thành công."));
        }
        catch (Exception ex)
        {
            var statusCode = ExceptionUtils.ExtractStatusCode(ex);
            var errorResponse = ExceptionUtils.CreateErrorResponse<SharedFileDto>(ex);
            return StatusCode(statusCode, errorResponse);
        }
    }

    [HttpPost("{id:guid}/share")]
    [ProducesResponseType(typeof(ApiResult<SharedFileDto>), 200)]
    [ProducesResponseType(typeof(ApiResult<SharedFileDto>), 404)]
    public async Task<IActionResult> CreateShareLink(Guid id)
    {
        try
        {
            var result = await _sharedFileService.CreateShareLinkAsync(_claimsService.CurrentUserId, id);
            return Ok(ApiResult<SharedFileDto>.Success(result, "200", "Tạo liên kết chia sẻ thành công."));
        }
        catch (Exception ex)
        {
            var statusCode = ExceptionUtils.ExtractStatusCode(ex);
            var errorResponse = ExceptionUtils.CreateErrorResponse<SharedFileDto>(ex);
            return StatusCode(statusCode, errorResponse);
        }
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(typeof(ApiResult<bool>), 200)]
    [ProducesResponseType(typeof(ApiResult<bool>), 403)]
    [ProducesResponseType(typeof(ApiResult<bool>), 404)]
    public async Task<IActionResult> Delete(Guid id)
    {
        try
        {
            var result = await _sharedFileService.DeleteAsync(
                _claimsService.CurrentUserId,
                id,
                HttpContext.RequestAborted);
            return Ok(ApiResult<bool>.Success(result, "200", "Xóa file thành công."));
        }
        catch (Exception ex)
        {
            var statusCode = ExceptionUtils.ExtractStatusCode(ex);
            var errorResponse = ExceptionUtils.CreateErrorResponse<bool>(ex);
            return StatusCode(statusCode, errorResponse);
        }
    }

    [HttpDelete("{id:guid}/share")]
    [ProducesResponseType(typeof(ApiResult<SharedFileDto>), 200)]
    [ProducesResponseType(typeof(ApiResult<SharedFileDto>), 404)]
    public async Task<IActionResult> RevokeShareLink(Guid id)
    {
        try
        {
            var result = await _sharedFileService.RevokeShareLinkAsync(_claimsService.CurrentUserId, id);
            return Ok(ApiResult<SharedFileDto>.Success(result, "200", "Thu hồi liên kết chia sẻ thành công."));
        }
        catch (Exception ex)
        {
            var statusCode = ExceptionUtils.ExtractStatusCode(ex);
            var errorResponse = ExceptionUtils.CreateErrorResponse<SharedFileDto>(ex);
            return StatusCode(statusCode, errorResponse);
        }
    }

    [HttpGet("links/{token}")]
    [ProducesResponseType(typeof(ApiResult<SharedFileDto>), 200)]
    [ProducesResponseType(typeof(ApiResult<SharedFileDto>), 404)]
    public async Task<IActionResult> GetByShareToken(string token)
    {
        try
        {
            var result = await _sharedFileService.GetByShareTokenAsync(token);
            return Ok(ApiResult<SharedFileDto>.Success(result, "200", "Lấy file chia sẻ thành công."));
        }
        catch (Exception ex)
        {
            var statusCode = ExceptionUtils.ExtractStatusCode(ex);
            var errorResponse = ExceptionUtils.CreateErrorResponse<SharedFileDto>(ex);
            return StatusCode(statusCode, errorResponse);
        }
    }

    [HttpGet("{id:guid}/view")]
    [ProducesResponseType(typeof(FileResult), 200)]
    public async Task<IActionResult> ViewMine(Guid id)
    {
        return await StreamOwnedFile(id, asAttachment: false);
    }

    [HttpGet("{id:guid}/download")]
    [ProducesResponseType(typeof(FileResult), 200)]
    public async Task<IActionResult> DownloadMine(Guid id)
    {
        return await StreamOwnedFile(id, asAttachment: true);
    }

    [HttpGet("links/{token}/view")]
    [ProducesResponseType(typeof(FileResult), 200)]
    public async Task<IActionResult> ViewByShareToken(string token)
    {
        return await StreamSharedFile(token, asAttachment: false);
    }

    [HttpGet("links/{token}/download")]
    [ProducesResponseType(typeof(FileResult), 200)]
    public async Task<IActionResult> DownloadByShareToken(string token)
    {
        return await StreamSharedFile(token, asAttachment: true);
    }

    private async Task<IActionResult> StreamOwnedFile(Guid id, bool asAttachment)
    {
        try
        {
            var download = await _sharedFileService.DownloadMyFileAsync(
                _claimsService.CurrentUserId,
                id,
                HttpContext.RequestAborted);
            HttpContext.Response.RegisterForDisposeAsync(download);
            if (asAttachment)
                return File(download.Stream, download.ContentType, download.FileName);
            return File(download.Stream, download.ContentType);
        }
        catch (Exception ex)
        {
            var statusCode = ExceptionUtils.ExtractStatusCode(ex);
            var errorResponse = ExceptionUtils.CreateErrorResponse<BlobDownloadResult>(ex);
            return StatusCode(statusCode, errorResponse);
        }
    }

    private async Task<IActionResult> StreamSharedFile(string token, bool asAttachment)
    {
        try
        {
            var download = await _sharedFileService.OpenByShareTokenAsync(
                token,
                asAttachment,
                HttpContext.RequestAborted);
            HttpContext.Response.RegisterForDisposeAsync(download);
            if (asAttachment)
                return File(download.Stream, download.ContentType, download.FileName);
            return File(download.Stream, download.ContentType);
        }
        catch (Exception ex)
        {
            var statusCode = ExceptionUtils.ExtractStatusCode(ex);
            var errorResponse = ExceptionUtils.CreateErrorResponse<BlobDownloadResult>(ex);
            return StatusCode(statusCode, errorResponse);
        }
    }
}
