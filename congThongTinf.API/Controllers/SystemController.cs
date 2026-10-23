using congThongTinf.Service.DTOs;
using congThongTinf.Service.Interfaces;
using congThongTinf.Service.Utils;
using Microsoft.AspNetCore.Mvc;

namespace congThongTinf.API.Controllers;

[ApiController]
[Route("api/system")]
public class SystemController : ControllerBase
{
    private readonly ISystemService _systemService;

    public SystemController(ISystemService systemService)
    {
        _systemService = systemService;
    }

    [HttpPost("seed-all-data")]
    public async Task<IActionResult> SeedData(CancellationToken cancellationToken)
    {
        try
        {
            var seeded = await _systemService.SeedAllAsync(cancellationToken);
            return Ok(ApiResult<object>.Success(new
            {
                Message = "Data seeded successfully.",
                seeded.RoleCount,
                seeded.AdminCount,
                seeded.JournalistCount,
                seeded.NewsCount,
                seeded.LeaderCount,
                seeded.BannerCount
            }));
        }
        catch (Exception ex)
        {
            var statusCode = ExceptionUtils.ExtractStatusCode(ex);
            return StatusCode(statusCode, ExceptionUtils.CreateErrorResponse<object>(ex));
        }
    }

    [HttpPost("seed-users-roles")]
    public async Task<IActionResult> SeedUsersRolesData()
    {
        try
        {
            var seeded = await _systemService.SeedUsersRolesAsync();
            return Ok(ApiResult<object>.Success(new
            {
                Message = "Roles, admin and journalist accounts seeded successfully.",
                seeded.RoleCount,
                seeded.AdminCount,
                seeded.JournalistCount
            }));
        }
        catch (Exception ex)
        {
            var statusCode = ExceptionUtils.ExtractStatusCode(ex);
            return StatusCode(statusCode, ExceptionUtils.CreateErrorResponse<object>(ex));
        }
    }

    [HttpPost("seed-leaders")]
    public async Task<IActionResult> SeedLeadersData()
    {
        try
        {
            var leaderCount = await _systemService.SeedLeadersAsync();
            return Ok(ApiResult<object>.Success(new
            {
                Message = "Leaders seeded successfully.",
                LeaderCount = leaderCount
            }));
        }
        catch (Exception ex)
        {
            var statusCode = ExceptionUtils.ExtractStatusCode(ex);
            return StatusCode(statusCode, ExceptionUtils.CreateErrorResponse<object>(ex));
        }
    }

    [HttpPost("seed-banners")]
    public async Task<IActionResult> SeedBannersData(CancellationToken cancellationToken)
    {
        try
        {
            var bannerCount = await _systemService.SeedBannersAsync(cancellationToken);
            return Ok(ApiResult<object>.Success(new
            {
                Message = "Banners seeded successfully.",
                BannerCount = bannerCount
            }));
        }
        catch (Exception ex)
        {
            var statusCode = ExceptionUtils.ExtractStatusCode(ex);
            return StatusCode(statusCode, ExceptionUtils.CreateErrorResponse<object>(ex));
        }
    }

    [HttpPost("seed-news")]
    public async Task<IActionResult> SeedNewsData(CancellationToken cancellationToken)
    {
        try
        {
            var seededCount = await _systemService.SeedNewsAsync(cancellationToken);
            return Ok(ApiResult<object>.Success(new
            {
                Message = "News seeded successfully.",
                NewsCount = seededCount
            }));
        }
        catch (Exception ex)
        {
            var statusCode = ExceptionUtils.ExtractStatusCode(ex);
            return StatusCode(statusCode, ExceptionUtils.CreateErrorResponse<object>(ex));
        }
    }

    [HttpPost("clear-database")]
    public async Task<IActionResult> ClearDatabaseData()
    {
        try
        {
            await _systemService.ClearDatabaseAsync();
            return Ok(ApiResult<object>.Success(new
            {
                Message = "Database cleared successfully."
            }));
        }
        catch (Exception ex)
        {
            var statusCode = ExceptionUtils.ExtractStatusCode(ex);
            return StatusCode(statusCode, ExceptionUtils.CreateErrorResponse<object>(ex));
        }
    }
}
