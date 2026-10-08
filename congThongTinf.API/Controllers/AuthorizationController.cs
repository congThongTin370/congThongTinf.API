using System.Security.Claims;
using congThongTinf.Service.Utils;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace congThongTinf.API.Controllers;

[Route("api/authorization")]
[ApiController]
public class AuthorizationController : ControllerBase
{
    [HttpGet("check-admin")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(ApiResult<object>), 200)]
    [ProducesResponseType(typeof(ApiResult<object>), 401)]
    [ProducesResponseType(typeof(ApiResult<object>), 403)]
    public IActionResult CheckAdmin()
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
}
