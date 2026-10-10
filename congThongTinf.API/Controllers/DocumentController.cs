using congThongTinf.Infrastructure.Commons;
using congThongTinf.Infrastructure.Interfaces;
using congThongTinf.Service.DTOs;
using congThongTinf.Service.Interfaces;
using congThongTinf.Service.Utils;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace congThongTinf.API.Controllers;

[Route("api/documents")]
[ApiController]
public class DocumentController : ControllerBase
{
    private readonly IDocumentService _documentService;
    private readonly IClaimsService _claimsService;

    public DocumentController(IDocumentService documentService, IClaimsService claimsService)
    {
        _documentService = documentService;
        _claimsService = claimsService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(ApiResult<Pagination<DocumentDto>>), 200)]
    public async Task<IActionResult> GetAll([FromQuery] DocumentQueryParameter param)
    {
        try
        {
            var result = await _documentService.GetAllDocumentsAsync(param);
            return Ok(ApiResult<Pagination<DocumentDto>>.Success(result, "200", "Lấy danh sách tài liệu nội bộ thành công."));
        }
        catch (Exception ex)
        {
            var statusCode = ExceptionUtils.ExtractStatusCode(ex);
            var errorResponse = ExceptionUtils.CreateErrorResponse<Pagination<DocumentDto>>(ex);
            return StatusCode(statusCode, errorResponse);
        }
    }

    /// <summary>
    /// Streams the file with Content-Disposition: attachment so the browser downloads it.
    /// Use as the href of a download button. Do not wrap in ApiResult.
    /// </summary>
    [HttpGet("{id:guid}/download")]
    [ProducesResponseType(typeof(FileResult), 200)]
    [ProducesResponseType(typeof(ApiResult<BlobDownloadResult>), 404)]
    public async Task<IActionResult> Download(Guid id)
    {
        try
        {
            var download = await _documentService.DownloadDocumentAsync(id, HttpContext.RequestAborted);
            HttpContext.Response.RegisterForDisposeAsync(download);
            return File(download.Stream, download.ContentType, download.FileName);
        }
        catch (Exception ex)
        {
            var statusCode = ExceptionUtils.ExtractStatusCode(ex);
            var errorResponse = ExceptionUtils.CreateErrorResponse<BlobDownloadResult>(ex);
            return StatusCode(statusCode, errorResponse);
        }
    }

    [Authorize(Roles = "Admin")]
    [HttpPost]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(ApiResult<DocumentDto>), 200)]
    [ProducesResponseType(typeof(ApiResult<DocumentDto>), 400)]
    public async Task<IActionResult> UploadInternalDocument([FromForm] UploadDocumentForm form)
    {
        try
        {
            var result = await _documentService.UploadInternalDocumentAsync(
                _claimsService.CurrentUserId,
                form,
                HttpContext.RequestAborted);
            return Ok(ApiResult<DocumentDto>.Success(result, "200", "Tải tài liệu nội bộ thành công."));
        }
        catch (Exception ex)
        {
            var statusCode = ExceptionUtils.ExtractStatusCode(ex);
            var errorResponse = ExceptionUtils.CreateErrorResponse<DocumentDto>(ex);
            return StatusCode(statusCode, errorResponse);
        }
    }
}
