using congThongTinf.Domain.Entities;
using congThongTinf.Service.DTOs;
using congThongTinf.Service.Interfaces;
using congThongTinf.Service.Utils;
using Microsoft.Extensions.Configuration;

namespace congThongTinf.Service.Services;

/// <summary>
/// Authentication for journalist account creation, login, and logout.
/// </summary>
public class AuthService : IAuthService
{
    private readonly ILoggerService _logger;
    private readonly IUnitOfWork _unitOfWork;

    public AuthService(ILoggerService logger, IUnitOfWork unitOfWork)
    {
        _logger = logger;
        _unitOfWork = unitOfWork;
    }

    public async Task<UserDto> CreateJournalistUser(CreateJournalistUserDto dto, Guid adminUserId)
    {
        _logger.Info($"[CreateJournalistUser] Admin {adminUserId} starts creating journalist {dto.Email}");

        var admin = await _unitOfWork.Users.FirstOrDefaultAsync(u => u.Id == adminUserId, u => u.Role!);
        if (admin == null)
            throw ErrorHelper.NotFound(ErrorMessages.AccountNotFound);

        if (!admin.IsActive || admin.Role?.Name != RoleType.Admin)
            throw ErrorHelper.Forbidden("Chỉ Admin mới được tạo tài khoản phóng viên.");

        if (string.IsNullOrWhiteSpace(dto.Username)
            || string.IsNullOrWhiteSpace(dto.Email)
            || string.IsNullOrWhiteSpace(dto.Password)
            || string.IsNullOrWhiteSpace(dto.FullName))
            throw ErrorHelper.BadRequest("Username, email, mật khẩu và họ tên là bắt buộc.");

        if (await _unitOfWork.Users.FirstOrDefaultAsync(u => u.Email == dto.Email) != null)
        {
            _logger.Warn($"[CreateJournalistUser] Email {dto.Email} already registered.");
            throw ErrorHelper.Conflict(ErrorMessages.AccountEmailAlreadyRegistered);
        }

        if (await _unitOfWork.Users.FirstOrDefaultAsync(u => u.Username == dto.Username) != null)
            throw ErrorHelper.Conflict("Tên đăng nhập đã được sử dụng.");

        var journalistRole = await _unitOfWork.Roles.FirstOrDefaultAsync(r => r.Name == RoleType.Journalist);
        if (journalistRole == null)
            throw ErrorHelper.NotFound("Chưa có vai trò Journalist. Hãy seed roles trước.");

        var user = new User
        {
            Username = dto.Username.Trim(),
            Email = dto.Email.Trim(),
            PasswordHash = new PasswordHasher().HashPassword(dto.Password) ?? string.Empty,
            FullName = dto.FullName.Trim(),
            RoleId = journalistRole.Id,
            IsActive = true
        };

        await _unitOfWork.Users.AddAsync(user);
        await _unitOfWork.SaveChangesAsync();

        _logger.Success($"[CreateJournalistUser] Journalist {user.Email} created successfully.");
        return ToUserDto(user, journalistRole.Name);
    }

    public async Task<LoginResponseDto> LoginAsync(LoginRequestDto loginDto, IConfiguration configuration)
    {
        _logger.Info($"[LoginAsync] Login attempt for {loginDto.Email}");

        var user = await _unitOfWork.Users.FirstOrDefaultAsync(
            u => u.Email == loginDto.Email,
            u => u.Role!);

        if (user == null)
            throw ErrorHelper.NotFound(ErrorMessages.AccountNotFound);

        if (!new PasswordHasher().VerifyPassword(loginDto.Password, user.PasswordHash))
            throw ErrorHelper.Unauthorized(ErrorMessages.AccountWrongPassword);

        if (!user.IsActive)
            throw ErrorHelper.Forbidden(ErrorMessages.AccountSuspendedOrBan);

        if (user.Role == null)
            throw ErrorHelper.NotFound(ErrorMessages.AccountInvalidRole);

        var accessToken = JwtUtils.GenerateJwtToken(
            user.Id,
            user.Email,
            user.Role.Name.ToString(),
            configuration,
            TimeSpan.FromMinutes(30));

        user.LastLoginAt = DateTime.UtcNow;
        await _unitOfWork.Users.Update(user);
        await _unitOfWork.SaveChangesAsync();

        _logger.Success($"[LoginAsync] User {loginDto.Email} authenticated successfully.");

        return new LoginResponseDto
        {
            AccessToken = accessToken,
            User = ToUserDto(user, user.Role.Name)
        };
    }

    public async Task<bool> LogoutAsync(Guid userId)
    {
        _logger.Info($"[LogoutAsync] Logout process initiated for user ID: {userId}");

        var user = await _unitOfWork.Users.FirstOrDefaultAsync(u => u.Id == userId);
        if (user == null)
            throw ErrorHelper.NotFound(ErrorMessages.AccountNotFound);

        if (!user.IsActive)
            throw ErrorHelper.Forbidden(ErrorMessages.AccountSuspendedOrBan);

        _logger.Info($"[LogoutAsync] Logout successful for user ID: {userId}.");
        return true;
    }

    private static UserDto ToUserDto(User user, RoleType roleName)
    {
        return new UserDto
        {
            UserId = user.Id,
            Username = user.Username,
            Email = user.Email,
            FullName = user.FullName,
            RoleName = roleName,
            IsActive = user.IsActive,
            CreatedAt = user.CreatedAt
        };
    }
}
