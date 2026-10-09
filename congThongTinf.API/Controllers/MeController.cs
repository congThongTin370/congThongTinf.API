using congThongTinf.Service.DTOs;
using congThongTinf.Service.Utils;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace congThongTinf.API.Controllers;

[Route("api/me")]
[ApiController]
[Authorize]
public class MeController : ControllerBase
{
    [HttpGet("role")]
    [ProducesResponseType(typeof(ApiResult<object>), 200)]
    [ProducesResponseType(typeof(ApiResult<object>), 401)]
    public IActionResult GetCurrentRole()
    {

        try
        {
            var role = User.FindFirstValue(ClaimTypes.Role);
            return Ok(ApiResult<object>.Success(new
            {
                Role = role,
                Email = User.FindFirstValue(ClaimTypes.Email) ?? User.FindFirstValue("email")
            }, "200", "Lấy thông tin role của tài khoản hiện tại thành công."));
        }
        catch (Exception ex)
        {
            var statusCode = ExceptionUtils.ExtractStatusCode(ex);
            var errorResponse = ExceptionUtils.CreateErrorResponse<UserDto>(ex);
            return StatusCode(statusCode, errorResponse);
        }


    }

    [HttpGet("check-admin")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(ApiResult<object>), 200)]
    [ProducesResponseType(typeof(ApiResult<object>), 401)]
    [ProducesResponseType(typeof(ApiResult<object>), 403)]
    public IActionResult CheckAdmin()
    {
        try
        {
            var role = User.FindFirstValue(ClaimTypes.Role);
            return Ok(ApiResult<object>.Success(new
            {
                IsAdmin = role == "Admin",
                UserId = User.FindFirstValue(ClaimTypes.NameIdentifier),
                Email = User.FindFirstValue(ClaimTypes.Email) ?? User.FindFirstValue("email"),
                Role = role
            }, "200", "Tài khoản hiện tại là Admin."));
        }
        catch (Exception ex)
        {
            var statusCode = ExceptionUtils.ExtractStatusCode(ex);
            var errorResponse = ExceptionUtils.CreateErrorResponse<UserDto>(ex);
            return StatusCode(statusCode, errorResponse);
        }

    }
}
