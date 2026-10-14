using congThongTinf.Domain.Entities;
using congThongTinf.Service.DTOs;
using congThongTinf.Service.Interfaces;
using congThongTinf.Service.Interfaces.Commons;
using congThongTinf.Service.Mappers;
using congThongTinf.Service.Utils;
using Microsoft.EntityFrameworkCore;

namespace congThongTinf.Service.Services;

public class AdminService : IAdminService
{
    private readonly ILoggerService _logger;
    private readonly IUnitOfWork _unitOfWork;

    public AdminService(IUnitOfWork unitOfWork, ILoggerService logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }


    public async Task<UserDto?> GetUserDetailsByIdAsync(Guid userId)
    {
        _logger.Info($"[GetUserByIdAsync] Admin requests detail for user {userId}");

        var user = await GetUserById(userId, true);
        if (user == null)
        {
            _logger.Warn($"[GetUserByIdAsync] User {userId} not found.");
            throw ErrorHelper.NotFound($"Người dùng với ID {userId} không tồn tại.");
        }

        return UserMapper.ToUserDto(user);
    }

    public async Task<UserDto?> UpdateProfileAsync(Guid userId, UpdateProfileDto dto)
    {
        _logger.Info($"[UpdateProfileAsync] Update profile for user {userId}");

        var user = await _unitOfWork.Users.GetByIdAsync(userId);
        if (user == null)
        {
            _logger.Warn($"[UpdateProfileAsync] User {userId} not found.");
            throw ErrorHelper.NotFound($"Người dùng với ID {userId} không tồn tại.");
        }

        if (!string.IsNullOrWhiteSpace(dto.FullName))
            user.FullName = dto.FullName;

        await _unitOfWork.Users.Update(user);
        await _unitOfWork.SaveChangesAsync();

        _logger.Success($"[UpdateProfileAsync] Profile updated for user {user.Email}");
        return UserMapper.ToUserDto(user);
    }

    /// <summary>
    ///     Gets a user by email.
    /// </summary>
    public async Task<User?> GetUserByEmail(string email, bool useCache = false)
    {
        var user = await _unitOfWork.Users.FirstOrDefaultAsync(u => u.Email == email && u.IsActive);
        return user;
    }

    /// <summary>
    ///     Gets a user by id.
    /// </summary>
    public async Task<User?> GetUserById(Guid id, bool useCache = false)
    {
        var user = await _unitOfWork.Users.FirstOrDefaultAsync(u => u.Id == id && u.IsActive);
        return user;
    }

    public async Task<DashboardBannerStatsDto> GetBannerDashboardSnapshotAsync()
    {
        var banners = _unitOfWork.Banners.GetQueryable().AsNoTracking();
        return new DashboardBannerStatsDto
        {
            Total = await banners.CountAsync(),
            Active = await banners.CountAsync(b => b.IsActive)
        };
    }

}
