using congThongTinf.Domain.Entities;
using congThongTinf.Service.DTOs;

namespace congThongTinf.Service.Interfaces;

public interface IAdminService
{
    Task<UserDto?> GetUserDetailsByIdAsync(Guid userId);
    Task<UserDto?> UpdateProfileAsync(Guid userId, UpdateProfileDto dto);

    Task<User?> GetUserByEmail(string email, bool useCache = false);
    Task<User?> GetUserById(Guid id, bool useCache = false);

    Task<BannerDto> UploadBannerAsync(Guid currentUserId, UploadBannerForm form, CancellationToken cancellationToken = default);
}