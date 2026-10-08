using congThongTinf.Infrastructure.Interfaces;
using congThongTinf.Service.DTOs;
using congThongTinf.Service.Interfaces;
using congThongTinf.Service.Utils;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace congThongTinf.API.Controllers;

[Route("api/auth")]
[ApiController]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly IClaimsService _claimsService;
    private readonly IConfiguration _configuration;

    public AuthController(IAuthService authService, IClaimsService claimsService, IConfiguration configuration)
    {
        _authService = authService;
        _claimsService = claimsService;
        _configuration = configuration;
    }

    [HttpPost("create-journalist")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(ApiResult<UserDto>), 200)]
    [ProducesResponseType(typeof(ApiResult<UserDto>), 403)]
    [ProducesResponseType(typeof(ApiResult<UserDto>), 409)]
    public async Task<IActionResult> CreateJournalistUser([FromBody] CreateJournalistUserDto dto)
    {
        try
        {
            var result = await _authService.CreateJournalistUser(dto, _claimsService.CurrentUserId);
            return Ok(ApiResult<UserDto>.Success(result, "200", "Tạo tài khoản phóng viên thành công."));
        }
        catch (Exception ex)
        {
            var statusCode = ExceptionUtils.ExtractStatusCode(ex);
            var errorResponse = ExceptionUtils.CreateErrorResponse<UserDto>(ex);
            return StatusCode(statusCode, errorResponse);
        }
    }

    [HttpPost("login")]
    [ProducesResponseType(typeof(ApiResult<LoginResponseDto>), 200)]
    [ProducesResponseType(typeof(ApiResult<LoginResponseDto>), 401)]
    public async Task<IActionResult> Login([FromBody] LoginRequestDto dto)
    {
        try
        {
            var result = await _authService.LoginAsync(dto, _configuration);
            return Ok(ApiResult<LoginResponseDto>.Success(result, "200", "Đăng nhập thành công."));
        }
        catch (Exception ex)
        {
            var statusCode = ExceptionUtils.ExtractStatusCode(ex);
            var errorResponse = ExceptionUtils.CreateErrorResponse<LoginResponseDto>(ex);
            return StatusCode(statusCode, errorResponse);
        }
    }

    [HttpPost("logout")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResult<object>), 200)]
    [ProducesResponseType(typeof(ApiResult<object>), 401)]
    public async Task<IActionResult> Logout()
    {
        try
        {
            var result = await _authService.LogoutAsync(_claimsService.CurrentUserId);
            return Ok(ApiResult<object>.Success(result, "200", "Đăng xuất thành công."));
        }
        catch (Exception ex)
        {
            var statusCode = ExceptionUtils.ExtractStatusCode(ex);
            var errorResponse = ExceptionUtils.CreateErrorResponse<object>(ex);
            return StatusCode(statusCode, errorResponse);
        }
    }
}
