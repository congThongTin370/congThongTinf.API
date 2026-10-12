using congThongTinf.Domain.Entities;
using congThongTinf.Service.DTOs;
using congThongTinf.Service.Interfaces;
using congThongTinf.Service.Interfaces.Commons;
using congThongTinf.Service.Mappers;
using congThongTinf.Service.Utils;
using Microsoft.AspNetCore.Http;

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


    public async Task<BannerDto> UploadBannerAsync(Guid currentUserId, UploadBannerForm form, CancellationToken cancellationToken = default)
    {
        _logger.Info($"[UploadBannerAsync] Admin {currentUserId} uploads web banner.");

        await EnsureActiveUserAsync(currentUserId);
        if (form == null)
            throw ErrorHelper.BadRequest("Dữ liệu không hợp lệ.");

        var file = ValidateUploadForm(form.File, BannerExtensions, "Banner phải là ảnh (jpg, jpeg, png, webp, gif).");

        var title = string.IsNullOrWhiteSpace(form.Title)
            ? Path.GetFileNameWithoutExtension(file.FileName)
            : form.Title.Trim();

        var upload = await _blobService.UploadFileAsync(file, "banners", cancellationToken);

        var banner = new Banner
        {
            Title = title,
            ImageUrl = upload.Url,
            FileKey = upload.Key,
            FileName = file.FileName,
            FileType = upload.ContentType,
            FileSize = upload.Size,
            DisplayOrder = form.DisplayOrder,
            IsActive = form.IsActive,
            UploadedByUserId = currentUserId
        };

        try
        {
            await _unitOfWork.Banners.AddAsync(banner);
            await _unitOfWork.SaveChangesAsync();
        }
        catch
        {
            await TryDeleteBlobAsync(upload.Key, cancellationToken);
            throw;
        }

        _logger.Success($"[UploadBannerAsync] Banner {banner.Id} uploaded by admin {currentUserId}.");
        return ToBannerDto(banner);
    }

    // ----------------- PRIVATE HELPER METHODS -----------------

    /// <summary>
    ///     Checks if a user exists in DB.
    /// </summary>
    private static readonly HashSet<string> BannerExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".jpg", ".jpeg", ".png", ".webp", ".gif"
    };

    private async Task EnsureActiveUserAsync(Guid userId)
    {
        if (userId == Guid.Empty)
            throw ErrorHelper.Unauthorized("Không xác định được tài khoản hiện tại.");

        var user = await GetUserById(userId);
        if (user == null)
        {
            _logger.Warn($"[EnsureActiveUserAsync] User {userId} not found.");
            throw ErrorHelper.NotFound($"Người dùng với ID {userId} không tồn tại.");
        }
    }

    private static IFormFile ValidateUploadForm(IFormFile? file, HashSet<string> allowedExtensions, string invalidTypeMessage)
    {
        if (file == null || file.Length == 0)
            throw ErrorHelper.BadRequest("File là bắt buộc.");

        var extension = Path.GetExtension(file.FileName);
        if (string.IsNullOrWhiteSpace(extension) || !allowedExtensions.Contains(extension))
            throw ErrorHelper.BadRequest(invalidTypeMessage);

        return file;
    }

    private async Task TryDeleteBlobAsync(string key, CancellationToken cancellationToken)
    {
        try
        {
            await _blobService.DeleteFileAsync(key, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.Warn($"[TryDeleteBlobAsync] Failed to roll back blob '{key}': {ex.Message}");
        }
    }

    private static BannerDto ToBannerDto(Banner banner)
    {
        return new BannerDto
        {
            Id = banner.Id,
            Title = banner.Title,
            ImageUrl = banner.ImageUrl,
            FileName = banner.FileName,
            FileType = banner.FileType,
            FileSize = banner.FileSize,
            DisplayOrder = banner.DisplayOrder,
            IsActive = banner.IsActive,
            UploadedByUserId = banner.UploadedByUserId,
            CreatedAt = banner.CreatedAt
        };
    }
}
