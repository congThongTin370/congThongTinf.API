using congThongTinf.Service.DTOs;
using congThongTinf.Service.Interfaces;
using congThongTinf.Service.Utils;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace congThongTinf.API.Controllers;

[Route("api/leaders")]
[ApiController]
public class LeaderController : ControllerBase
{
    private readonly ILeaderService _leaderService;

    public LeaderController(ILeaderService leaderService)
    {
        _leaderService = leaderService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(ApiResult<List<LeaderDto>>), 200)]
    public async Task<IActionResult> GetAll()
    {
        try
        {
            var result = await _leaderService.GetAllLeadersAsync();
            return Ok(ApiResult<List<LeaderDto>>.Success(result, "200", "Lấy danh sách thủ trưởng thành công."));
        }
        catch (Exception ex)
        {
            var statusCode = ExceptionUtils.ExtractStatusCode(ex);
            var errorResponse = ExceptionUtils.CreateErrorResponse<List<LeaderDto>>(ex);
            return StatusCode(statusCode, errorResponse);
        }
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ApiResult<LeaderDto>), 200)]
    [ProducesResponseType(typeof(ApiResult<LeaderDto>), 404)]
    public async Task<IActionResult> GetById(Guid id)
    {
        try
        {
            var result = await _leaderService.GetLeaderByIdAsync(id);
            return Ok(ApiResult<LeaderDto>.Success(result, "200", "Lấy thủ trưởng thành công."));
        }
        catch (Exception ex)
        {
            var statusCode = ExceptionUtils.ExtractStatusCode(ex);
            var errorResponse = ExceptionUtils.CreateErrorResponse<LeaderDto>(ex);
            return StatusCode(statusCode, errorResponse);
        }
    }

    [Authorize(Roles = "Admin")]
    [HttpPost]
    [ProducesResponseType(typeof(ApiResult<LeaderDto>), 200)]
    [ProducesResponseType(typeof(ApiResult<LeaderDto>), 400)]
    public async Task<IActionResult> Create([FromBody] CreateLeaderDto dto)
    {
        try
        {
            var result = await _leaderService.CreateLeaderAsync(dto);
            return Ok(ApiResult<LeaderDto>.Success(result, "200", "Tạo thủ trưởng thành công."));
        }
        catch (Exception ex)
        {
            var statusCode = ExceptionUtils.ExtractStatusCode(ex);
            var errorResponse = ExceptionUtils.CreateErrorResponse<LeaderDto>(ex);
            return StatusCode(statusCode, errorResponse);
        }
    }

    [Authorize(Roles = "Admin")]
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(ApiResult<LeaderDto>), 200)]
    [ProducesResponseType(typeof(ApiResult<LeaderDto>), 400)]
    [ProducesResponseType(typeof(ApiResult<LeaderDto>), 404)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateLeaderDto dto)
    {
        try
        {
            var result = await _leaderService.UpdateLeaderAsync(id, dto);
            return Ok(ApiResult<LeaderDto>.Success(result, "200", "Cập nhật thủ trưởng thành công."));
        }
        catch (Exception ex)
        {
            var statusCode = ExceptionUtils.ExtractStatusCode(ex);
            var errorResponse = ExceptionUtils.CreateErrorResponse<LeaderDto>(ex);
            return StatusCode(statusCode, errorResponse);
        }
    }

    [Authorize(Roles = "Admin")]
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(typeof(ApiResult<bool>), 200)]
    [ProducesResponseType(typeof(ApiResult<bool>), 404)]
    public async Task<IActionResult> Delete(Guid id)
    {
        try
        {
            var result = await _leaderService.DeleteLeaderAsync(id);
            return Ok(ApiResult<bool>.Success(result, "200", "Xóa thủ trưởng thành công."));
        }
        catch (Exception ex)
        {
            var statusCode = ExceptionUtils.ExtractStatusCode(ex);
            var errorResponse = ExceptionUtils.CreateErrorResponse<bool>(ex);
            return StatusCode(statusCode, errorResponse);
        }
    }
}
