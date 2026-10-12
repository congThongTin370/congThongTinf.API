using congThongTinf.Infrastructure.Commons;
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
    private readonly IUserService _userService;
    private readonly IClaimsService _claimsService;

    public AdminController(IAdminService adminService, IUserService userService, IClaimsService claimsService)
    {
        _adminService = adminService;
        _userService = userService;
        _claimsService = claimsService;
    }

    [HttpGet("users")]
    [ProducesResponseType(typeof(ApiResult<Pagination<UserDto>>), 200)]
    public async Task<IActionResult> GetAllUsers([FromQuery] UserQueryParameter param)
    {
        try
        {
            var result = await _userService.GetAllUsersAsync(param);
            return Ok(ApiResult<Pagination<UserDto>>.Success(result, "200", "Lấy danh sách người dùng thành công."));
        }
        catch (Exception ex)
        {
            var statusCode = ExceptionUtils.ExtractStatusCode(ex);
            var errorResponse = ExceptionUtils.CreateErrorResponse<Pagination<UserDto>>(ex);
            return StatusCode(statusCode, errorResponse);
        }
    }

    [HttpGet("users/{id:guid}")]
    [ProducesResponseType(typeof(ApiResult<UserDto>), 200)]
    [ProducesResponseType(typeof(ApiResult<UserDto>), 404)]
    public async Task<IActionResult> GetUserById(Guid id)
    {
        try
        {
            var result = await _userService.GetUserByIdAsync(id);
            return Ok(ApiResult<UserDto>.Success(result, "200", "Lấy thông tin người dùng thành công."));
        }
        catch (Exception ex)
        {
            var statusCode = ExceptionUtils.ExtractStatusCode(ex);
            var errorResponse = ExceptionUtils.CreateErrorResponse<UserDto>(ex);
            return StatusCode(statusCode, errorResponse);
        }
    }

    [HttpPost("users")]
    [ProducesResponseType(typeof(ApiResult<UserDto>), 200)]
    [ProducesResponseType(typeof(ApiResult<UserDto>), 400)]
    [ProducesResponseType(typeof(ApiResult<UserDto>), 409)]
    public async Task<IActionResult> CreateUser([FromBody] UserCreateDto dto)
    {
        try
        {
            var result = await _userService.CreateUserAsync(dto);
            return Ok(ApiResult<UserDto>.Success(result, "200", "Tạo tài khoản thành công."));
        }
        catch (Exception ex)
        {
            var statusCode = ExceptionUtils.ExtractStatusCode(ex);
            var errorResponse = ExceptionUtils.CreateErrorResponse<UserDto>(ex);
            return StatusCode(statusCode, errorResponse);
        }
    }

    [HttpPut("users/{id:guid}")]
    [ProducesResponseType(typeof(ApiResult<UserDto>), 200)]
    [ProducesResponseType(typeof(ApiResult<UserDto>), 400)]
    [ProducesResponseType(typeof(ApiResult<UserDto>), 404)]
    public async Task<IActionResult> UpdateUser(Guid id, [FromBody] UserUpdateDto dto)
    {
        try
        {
            var result = await _userService.UpdateUserAsync(id, dto);
            return Ok(ApiResult<UserDto>.Success(result, "200", "Cập nhật người dùng thành công."));
        }
        catch (Exception ex)
        {
            var statusCode = ExceptionUtils.ExtractStatusCode(ex);
            var errorResponse = ExceptionUtils.CreateErrorResponse<UserDto>(ex);
            return StatusCode(statusCode, errorResponse);
        }
    }

    [HttpDelete("users/{id:guid}")]
    [ProducesResponseType(typeof(ApiResult<bool>), 200)]
    [ProducesResponseType(typeof(ApiResult<bool>), 400)]
    [ProducesResponseType(typeof(ApiResult<bool>), 404)]
    public async Task<IActionResult> DeleteUser(Guid id)
    {
        try
        {
            var result = await _userService.DeleteUserAsync(_claimsService.CurrentUserId, id);
            return Ok(ApiResult<bool>.Success(result, "200", "Xóa người dùng thành công."));
        }
        catch (Exception ex)
        {
            var statusCode = ExceptionUtils.ExtractStatusCode(ex);
            var errorResponse = ExceptionUtils.CreateErrorResponse<bool>(ex);
            return StatusCode(statusCode, errorResponse);
        }
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
