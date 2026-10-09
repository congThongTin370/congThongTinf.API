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
    [ProducesResponseType(typeof(ApiResult<List<NewsDto>>), 200)]
    [ProducesResponseType(typeof(ApiResult<List<NewsDto>>), 400)]
    public async Task<IActionResult> GetAll([FromQuery] NewsQueryDto query)
    {
        try
        {
            var result = await _newsService.GetAllNewsAsync(query);
            return Ok(ApiResult<List<NewsDto>>.Success(result, "200", "Lấy danh sách bài báo thành công."));
        }
        catch (Exception ex)
        {
            var statusCode = ExceptionUtils.ExtractStatusCode(ex);
            var errorResponse = ExceptionUtils.CreateErrorResponse<List<NewsDto>>(ex);
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

    [Authorize]
    [HttpPost]
    [ProducesResponseType(typeof(ApiResult<NewsDto>), 200)]
    [ProducesResponseType(typeof(ApiResult<NewsDto>), 403)]
    public async Task<IActionResult> Create([FromBody] CreateNewsDto dto)
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

    [Authorize]
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(ApiResult<NewsDto>), 200)]
    [ProducesResponseType(typeof(ApiResult<NewsDto>), 403)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateNewsDto dto)
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
