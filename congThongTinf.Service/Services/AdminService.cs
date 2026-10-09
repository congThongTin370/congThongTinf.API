using congThongTinf.Domain.Entities;
using congThongTinf.Infrastructure.Commons;
using congThongTinf.Service.DTOs;
using congThongTinf.Service.Interfaces;
using congThongTinf.Service.Interfaces.Commons;
using congThongTinf.Service.Mappers;
using congThongTinf.Service.Utils;
using Microsoft.EntityFrameworkCore;

namespace congThongTinf.Service.Services;

public class AdminService : IAdminService
{
    private readonly IBlobService _blobService;
    private readonly ILoggerService _logger;
    private readonly IUnitOfWork _unitOfWork;

    public AdminService(
        IUnitOfWork unitOfWork,
        ILoggerService logger,
        IBlobService blobService)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
        _blobService = blobService;
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


    //Admin methods
    public async Task<Pagination<UserDto>> GetAllUsersAsync(UserQueryParameter param)
    {
        _logger.Info($"[GetAllUsersAsync] Admin requests user list. Page: {param.PageIndex}, Size: {param.PageSize}");

        var query = _unitOfWork.Users.GetQueryable()
            .Include(u => u.Role)
            .AsNoTracking();

        // Filter
        if (!string.IsNullOrWhiteSpace(param.Search))
        {
            var keyword = param.Search.Trim().ToLower();
            query = query.Where(u =>
                (!string.IsNullOrEmpty(u.FullName) && u.FullName.ToLower().Contains(keyword)) ||
                (!string.IsNullOrEmpty(u.Email) && u.Email.ToLower().Contains(keyword)));
        }

        if (param.IsActive.HasValue)
            query = query.Where(u => u.IsActive == param.IsActive.Value);
        if (param.RoleName.HasValue)
            query = query.Where(u => u.Role != null && u.Role.Name == param.RoleName.Value);

        var total = await query.CountAsync();
        if (total == 0)
            _logger.Info("Không tìm thấy người dùng nào.");

        List<User> users;
        if (param.PageIndex == 0)
            users = await query.ToListAsync();
        else
            users = await query
                .Skip((param.PageIndex - 1) * param.PageSize)
                .Take(param.PageSize)
                .ToListAsync();

        var userDtos = users.Select(UserMapper.ToUserDto).ToList();
        return new Pagination<UserDto>(userDtos, total, param.PageIndex, param.PageSize);
    }


    public async Task<UserDto?> CreateUserAsync(UserCreateDto dto)
    {
        _logger.Info($"[CreateUserAsync] Admin creates user {dto.Email}");

        if (await UserExistsAsync(dto.Email))
        {
            _logger.Warn($"[CreateUserAsync] Email {dto.Email} already exists.");
            throw ErrorHelper.Conflict($"Email {dto.Email} đã tồn tại trong hệ thống.");
        }

        var role = await _unitOfWork.Roles.FirstOrDefaultAsync(r => r.Name == dto.RoleName);
        if (role == null)
            throw ErrorHelper.NotFound($"Chưa có vai trò {dto.RoleName}. Hãy seed roles trước.");

        var hashedPassword = new PasswordHasher().HashPassword(dto.Password);
        var user = new User
        {
            Username = dto.Username,
            Email = dto.Email,
            PasswordHash = hashedPassword ?? string.Empty,
            FullName = dto.FullName,
            RoleId = role.Id,
        };

        await _unitOfWork.Users.AddAsync(user);
        await _unitOfWork.SaveChangesAsync();

        _logger.Success($"[CreateUserAsync] User {user.Email} created by admin.");
        user.Role = role;
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


    // ----------------- PRIVATE HELPER METHODS -----------------

    /// <summary>
    ///     Checks if a user exists in DB.
    /// </summary>
    private async Task<bool> UserExistsAsync(string email)
    {
        var existingUser = await _unitOfWork.Users.FirstOrDefaultAsync(u => u.Email == email);
        return existingUser != null;
    }

}
