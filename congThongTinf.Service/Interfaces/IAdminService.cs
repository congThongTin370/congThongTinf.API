using congThongTinf.Domain.Entities;
using congThongTinf.Infrastructure.Commons;
using congThongTinf.Service.DTOs;

namespace congThongTinf.Service.Interfaces;

public interface IAdminService
{
    Task<UserDto?> GetUserDetailsByIdAsync(Guid userId);
    Task<UserDto?> UpdateProfileAsync(Guid userId, UpdateProfileDto dto);

    //admin methods
    Task<UserDto?> CreateUserAsync(UserCreateDto dto);
    Task<Pagination<UserDto>> GetAllUsersAsync(UserQueryParameter param);
    Task<User?> GetUserByEmail(string email, bool useCache = false);
    Task<User?> GetUserById(Guid id, bool useCache = false);

    Task<BannerDto> UploadBannerAsync(Guid currentUserId, UploadBannerForm form, CancellationToken cancellationToken = default);
}