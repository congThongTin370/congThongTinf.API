using congThongTinf.Service.DTOs;
using congThongTinf.Service.Interfaces;
using congThongTinf.Service.Utils;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace congThongTinf.API.Controllers;

[Route("api/news")]
[ApiController]
[Authorize]
public class NewsController : ControllerBase
{
    private readonly INewsService _newsService;

    public NewsController(INewsService newsService)
    {
        _newsService = newsService;
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
}
