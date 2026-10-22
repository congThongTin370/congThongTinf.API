using congThongTinf.Service.DTOs;
using congThongTinf.Service.Interfaces.Commons;
using congThongTinf.Service.Utils;
using Microsoft.AspNetCore.Mvc;

namespace congThongTinf.API.Controllers;

[Route("api/blobs")]
[ApiController]
public class BlobController : ControllerBase
{
    private readonly IBlobService _blobService;

    public BlobController(IBlobService blobService)
    {
        _blobService = blobService;
    }

    /// <summary>
    /// Test endpoint: upload a file to object storage.
    /// </summary>
    [HttpPost("upload")]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(ApiResult<UploadFileResultDto>), 200)]
    [ProducesResponseType(typeof(ApiResult<UploadFileResultDto>), 400)]
    public async Task<IActionResult> Upload([FromForm] UploadFileForm form, [FromQuery] string folder = "test")
    {
        try
        {
            var result = await _blobService.UploadFileAsync(form?.File!, folder, HttpContext.RequestAborted);
            return Ok(ApiResult<UploadFileResultDto>.Success(result, "200", "Upload file thành công."));
        }
        catch (Exception ex)
        {
            var statusCode = ExceptionUtils.ExtractStatusCode(ex);
            var errorResponse = ExceptionUtils.CreateErrorResponse<UploadFileResultDto>(ex);
            return StatusCode(statusCode, errorResponse);
        }
    }

    /// <summary>
    /// Test endpoint: delete an object from object storage by key.
    /// </summary>
    [HttpDelete]
    [ProducesResponseType(typeof(ApiResult<bool>), 200)]
    public async Task<IActionResult> Delete([FromQuery] string key)
    {
        try
        {
            await _blobService.DeleteFileAsync(key, HttpContext.RequestAborted);
            return Ok(ApiResult<bool>.Success(true, "200", "Xóa file thành công."));
        }
        catch (Exception ex)
        {
            var statusCode = ExceptionUtils.ExtractStatusCode(ex);
            var errorResponse = ExceptionUtils.CreateErrorResponse<bool>(ex);
            return StatusCode(statusCode, errorResponse);
        }
    }
}
