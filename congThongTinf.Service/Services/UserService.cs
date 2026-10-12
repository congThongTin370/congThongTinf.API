using congThongTinf.Domain.Entities;
using congThongTinf.Infrastructure.Commons;
using congThongTinf.Service.DTOs;
using congThongTinf.Service.Interfaces;
using congThongTinf.Service.Interfaces.Commons;
using congThongTinf.Service.Mappers;
using congThongTinf.Service.Utils;
using Microsoft.EntityFrameworkCore;

namespace congThongTinf.Service.Services;

public class UserService : IUserService
{
    private readonly ILoggerService _logger;
    private readonly IUnitOfWork _unitOfWork;

    public UserService(ILoggerService logger, IUnitOfWork unitOfWork)
    {
        _logger = logger;
        _unitOfWork = unitOfWork;
    }

    public async Task<Pagination<UserDto>> GetAllUsersAsync(UserQueryParameter? param)
    {
        param ??= new UserQueryParameter();
        if (param.PageSize <= 0)
            param.PageSize = 10;

        _logger.Info($"[GetAllUsers] Page: {param.PageIndex}, Size: {param.PageSize}");

        var query = _unitOfWork.Users.GetQueryable()
            .Include(u => u.Role)
            .AsNoTracking();

        if (!string.IsNullOrWhiteSpace(param.Search))
        {
            var keyword = param.Search.Trim().ToLower();
            query = query.Where(u =>
                (!string.IsNullOrEmpty(u.FullName) && u.FullName.ToLower().Contains(keyword)) ||
                (!string.IsNullOrEmpty(u.Email) && u.Email.ToLower().Contains(keyword)) ||
                (!string.IsNullOrEmpty(u.Username) && u.Username.ToLower().Contains(keyword)));
        }

        if (param.IsActive.HasValue)
            query = query.Where(u => u.IsActive == param.IsActive.Value);

        if (param.RoleName.HasValue)
        {
            if (!Enum.IsDefined(param.RoleName.Value))
                throw ErrorHelper.BadRequest("Vai trò không hợp lệ.");

            query = query.Where(u => u.Role != null && u.Role.Name == param.RoleName.Value);
        }

        query = param.Desc
            ? query.OrderByDescending(u => u.CreatedAt)
            : query.OrderBy(u => u.CreatedAt);

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

        _logger.Success($"[GetAllUsers] Returned {users.Count} of {total} users.");
        return new Pagination<UserDto>(users.Select(UserMapper.ToUserDto).ToList(), total, param.PageIndex, param.PageSize);
    }

    public async Task<DashboardUserStatsDto> GetDashboardSnapshotAsync(DateTime from, DateTime toExclusive)
    {
        var users = _unitOfWork.Users.GetQueryable().AsNoTracking();

        return new DashboardUserStatsDto
        {
            Total = await users.CountAsync(),
            Active = await users.CountAsync(u => u.IsActive),
            Inactive = await users.CountAsync(u => !u.IsActive),
            Admin = await users.CountAsync(u => u.Role != null && u.Role.Name == RoleType.Admin),
            Journalist = await users.CountAsync(u => u.Role != null && u.Role.Name == RoleType.Journalist),
            LoggedInInRange = await users.CountAsync(u =>
                u.LastLoginAt.HasValue && u.LastLoginAt.Value >= from && u.LastLoginAt.Value < toExclusive)
        };
    }

    public async Task<UserDto> GetUserByIdAsync(Guid id)
    {
        _logger.Info($"[GetUserById] Requests user {id}");

        var user = await GetUserWithRoleAsync(id);
        if (user == null)
        {
            _logger.Warn($"[GetUserById] User {id} not found.");
            throw ErrorHelper.NotFound($"Người dùng với ID {id} không tồn tại.");
        }

        return UserMapper.ToUserDto(user);
    }

    public async Task<UserDto> CreateUserAsync(UserCreateDto dto)
    {
        _logger.Info($"[CreateUser] Admin creates user {dto.Email}");

        if (string.IsNullOrWhiteSpace(dto.Username)
            || string.IsNullOrWhiteSpace(dto.Email)
            || string.IsNullOrWhiteSpace(dto.Password)
            || string.IsNullOrWhiteSpace(dto.FullName))
            throw ErrorHelper.BadRequest("Username, email, mật khẩu và họ tên là bắt buộc.");

        if (!Enum.IsDefined(dto.RoleName))
            throw ErrorHelper.BadRequest("Vai trò không hợp lệ.");

        var username = dto.Username.Trim();
        var email = dto.Email.Trim();

        if (await _unitOfWork.Users.FirstOrDefaultAsync(u => u.Email == email) != null)
        {
            _logger.Warn($"[CreateUser] Email {email} already exists.");
            throw ErrorHelper.Conflict(ErrorMessages.AccountEmailAlreadyRegistered);
        }

        if (await _unitOfWork.Users.FirstOrDefaultAsync(u => u.Username == username) != null)
            throw ErrorHelper.Conflict("Tên đăng nhập đã được sử dụng.");

        var role = await _unitOfWork.Roles.FirstOrDefaultAsync(r => r.Name == dto.RoleName);
        if (role == null)
            throw ErrorHelper.NotFound($"Chưa có vai trò {dto.RoleName}. Hãy seed roles trước.");

        var user = new User
        {
            Username = username,
            Email = email,
            PasswordHash = new PasswordHasher().HashPassword(dto.Password) ?? string.Empty,
            FullName = dto.FullName.Trim(),
            RoleId = role.Id,
            IsActive = true
        };

        await _unitOfWork.Users.AddAsync(user);
        await _unitOfWork.SaveChangesAsync();

        user.Role = role;
        _logger.Success($"[CreateUser] User {user.Email} created.");
        return UserMapper.ToUserDto(user);
    }

    public async Task<UserDto> UpdateUserAsync(Guid id, UserUpdateDto dto)
    {
        _logger.Info($"[UpdateUser] Starts updating user {id}.");

        var user = await GetUserWithRoleAsync(id);
        if (user == null)
        {
            _logger.Warn($"[UpdateUser] User {id} not found.");
            throw ErrorHelper.NotFound($"Người dùng với ID {id} không tồn tại.");
        }

        if (dto.Username != null)
        {
            if (string.IsNullOrWhiteSpace(dto.Username))
                throw ErrorHelper.BadRequest("Username không được để trống.");

            var username = dto.Username.Trim();
            if (!string.Equals(username, user.Username, StringComparison.OrdinalIgnoreCase)
                && await _unitOfWork.Users.FirstOrDefaultAsync(u => u.Username == username && u.Id != id) != null)
                throw ErrorHelper.Conflict("Tên đăng nhập đã được sử dụng.");

            user.Username = username;
        }

        if (dto.Email != null)
        {
            if (string.IsNullOrWhiteSpace(dto.Email))
                throw ErrorHelper.BadRequest("Email không được để trống.");

            var email = dto.Email.Trim();
            if (!string.Equals(email, user.Email, StringComparison.OrdinalIgnoreCase)
                && await _unitOfWork.Users.FirstOrDefaultAsync(u => u.Email == email && u.Id != id) != null)
                throw ErrorHelper.Conflict(ErrorMessages.AccountEmailAlreadyRegistered);

            user.Email = email;
        }

        if (dto.FullName != null)
        {
            if (string.IsNullOrWhiteSpace(dto.FullName))
                throw ErrorHelper.BadRequest("Họ tên không được để trống.");

            user.FullName = dto.FullName.Trim();
        }

        if (!string.IsNullOrWhiteSpace(dto.Password))
            user.PasswordHash = new PasswordHasher().HashPassword(dto.Password) ?? string.Empty;

        if (dto.RoleName.HasValue)
        {
            if (!Enum.IsDefined(dto.RoleName.Value))
                throw ErrorHelper.BadRequest("Vai trò không hợp lệ.");

            var role = await _unitOfWork.Roles.FirstOrDefaultAsync(r => r.Name == dto.RoleName.Value);
            if (role == null)
                throw ErrorHelper.NotFound($"Chưa có vai trò {dto.RoleName.Value}. Hãy seed roles trước.");

            user.RoleId = role.Id;
            user.Role = role;
        }

        if (dto.IsActive.HasValue)
            user.IsActive = dto.IsActive.Value;

        await _unitOfWork.Users.Update(user);
        await _unitOfWork.SaveChangesAsync();

        _logger.Success($"[UpdateUser] User {id} updated.");
        return UserMapper.ToUserDto(user);
    }

    public async Task<bool> DeleteUserAsync(Guid currentUserId, Guid id)
    {
        _logger.Info($"[DeleteUser] User {currentUserId} starts deleting user {id}.");

        if (currentUserId == id)
            throw ErrorHelper.BadRequest("Không thể xóa tài khoản đang đăng nhập.");

        var user = await _unitOfWork.Users.GetByIdAsync(id);
        if (user == null || !user.IsActive)
        {
            _logger.Warn($"[DeleteUser] User {id} not found.");
            throw ErrorHelper.NotFound($"Người dùng với ID {id} không tồn tại.");
        }

        user.IsActive = false;
        await _unitOfWork.Users.Update(user);
        await _unitOfWork.SaveChangesAsync();

        _logger.Success($"[DeleteUser] User {id} soft deleted.");
        return true;
    }

    private async Task<User?> GetUserWithRoleAsync(Guid id)
    {
        return await _unitOfWork.Users.GetQueryable()
            .Include(u => u.Role)
            .FirstOrDefaultAsync(u => u.Id == id);
    }
}
