using congThongTinf.Infrastructure.Commons;
using congThongTinf.Infrastructure.Interfaces;
using congThongTinf.Service.DTOs;
using congThongTinf.Service.Interfaces;
using congThongTinf.Service.Utils;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace congThongTinf.API.Controllers;

[Route("api/news")]
[ApiController]
public class NewsController : ControllerBase
{
    private readonly INewsService _newsService;
    private readonly IClaimsService _claimsService;

    public NewsController(INewsService newsService, IClaimsService claimsService)
    {
        _newsService = newsService;
        _claimsService = claimsService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(ApiResult<Pagination<NewsDto>>), 200)]
    [ProducesResponseType(typeof(ApiResult<Pagination<NewsDto>>), 400)]
    public async Task<IActionResult> GetAll([FromQuery] NewsQueryDto query)
    {
        try
        {
            var result = await _newsService.GetAllNewsAsync(query);
            return Ok(ApiResult<Pagination<NewsDto>>.Success(result, "200", "Lấy danh sách bài báo thành công."));
        }
        catch (Exception ex)
        {
            var statusCode = ExceptionUtils.ExtractStatusCode(ex);
            var errorResponse = ExceptionUtils.CreateErrorResponse<Pagination<NewsDto>>(ex);
            return StatusCode(statusCode, errorResponse);
        }
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ApiResult<NewsDto>), 200)]
    [ProducesResponseType(typeof(ApiResult<NewsDto>), 404)]
    public async Task<IActionResult> GetById(Guid id)
    {
        try
        {
            var result = await _newsService.GetNewsByIdAsync(id);
            return Ok(ApiResult<NewsDto>.Success(result, "200", "Lấy bài báo thành công."));
        }
        catch (Exception ex)
        {
            var statusCode = ExceptionUtils.ExtractStatusCode(ex);
            var errorResponse = ExceptionUtils.CreateErrorResponse<NewsDto>(ex);
            return StatusCode(statusCode, errorResponse);
        }
    }

    [HttpPost("{id:guid}/view")]
    [ProducesResponseType(typeof(ApiResult<int>), 200)]
    [ProducesResponseType(typeof(ApiResult<int>), 404)]
    public async Task<IActionResult> IncrementViewCount(Guid id)
    {
        try
        {
            var result = await _newsService.IncrementViewCountAsync(id);
            return Ok(ApiResult<int>.Success(result, "200", "Tăng lượt xem thành công."));
        }
        catch (Exception ex)
        {
            var statusCode = ExceptionUtils.ExtractStatusCode(ex);
            var errorResponse = ExceptionUtils.CreateErrorResponse<int>(ex);
            return StatusCode(statusCode, errorResponse);
        }
    }

    [Authorize(Roles = "Journalist")]
    [HttpPost]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(ApiResult<NewsDto>), 200)]
    [ProducesResponseType(typeof(ApiResult<NewsDto>), 403)]
    public async Task<IActionResult> Create([FromForm] CreateNewsDto dto)
    {
        try
        {
            var result = await _newsService.CreateNewsAsync(_claimsService.CurrentUserId, dto);
            return Ok(ApiResult<NewsDto>.Success(result, "200", "Tạo bài báo thành công."));
        }
        catch (Exception ex)
        {
            var statusCode = ExceptionUtils.ExtractStatusCode(ex);
            var errorResponse = ExceptionUtils.CreateErrorResponse<NewsDto>(ex);
            return StatusCode(statusCode, errorResponse);
        }
    }

    [Authorize(Roles = "Journalist")]
    [HttpPost("images")]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(ApiResult<NewsImageDto>), 200)]
    [ProducesResponseType(typeof(ApiResult<NewsImageDto>), 400)]
    [ProducesResponseType(typeof(ApiResult<NewsImageDto>), 403)]
    public async Task<IActionResult> UploadImage([FromForm] UploadFileForm form)
    {
        try
        {
            var result = await _newsService.UploadImageAsync(
                _claimsService.CurrentUserId,
                form?.File,
                HttpContext.RequestAborted);
            return Ok(ApiResult<NewsImageDto>.Success(result, "200", "Tải ảnh thành công."));
        }
        catch (Exception ex)
        {
            var statusCode = ExceptionUtils.ExtractStatusCode(ex);
            var errorResponse = ExceptionUtils.CreateErrorResponse<NewsImageDto>(ex);
            return StatusCode(statusCode, errorResponse);
        }
    }

    [Authorize]
    [HttpPut("{id:guid}")]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(ApiResult<NewsDto>), 200)]
    [ProducesResponseType(typeof(ApiResult<NewsDto>), 403)]
    public async Task<IActionResult> Update(Guid id, [FromForm] UpdateNewsDto dto)
    {
        try
        {
            var result = await _newsService.UpdateNewsAsync(_claimsService.CurrentUserId, id, dto);
            return Ok(ApiResult<NewsDto>.Success(result, "200", "Cập nhật bài báo thành công."));
        }
        catch (Exception ex)
        {
            var statusCode = ExceptionUtils.ExtractStatusCode(ex);
            var errorResponse = ExceptionUtils.CreateErrorResponse<NewsDto>(ex);
            return StatusCode(statusCode, errorResponse);
        }
    }

    [Authorize]
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(typeof(ApiResult<bool>), 200)]
    [ProducesResponseType(typeof(ApiResult<bool>), 403)]
    public async Task<IActionResult> Delete(Guid id)
    {
        try
        {
            var result = await _newsService.DeleteNewsAsync(_claimsService.CurrentUserId, id);
            return Ok(ApiResult<bool>.Success(result, "200", "Xóa bài báo thành công."));
        }
        catch (Exception ex)
        {
            var statusCode = ExceptionUtils.ExtractStatusCode(ex);
            var errorResponse = ExceptionUtils.CreateErrorResponse<bool>(ex);
            return StatusCode(statusCode, errorResponse);
        }
    }
}
