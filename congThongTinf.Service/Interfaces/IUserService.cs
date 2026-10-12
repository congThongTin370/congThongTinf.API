using congThongTinf.Infrastructure.Commons;
using congThongTinf.Service.DTOs;

namespace congThongTinf.Service.Interfaces;

public interface IUserService
{
    Task<Pagination<UserDto>> GetAllUsersAsync(UserQueryParameter? param);

    Task<UserDto> GetUserByIdAsync(Guid id);

    Task<UserDto> CreateUserAsync(UserCreateDto dto);

    Task<UserDto> UpdateUserAsync(Guid id, UserUpdateDto dto);

    Task<bool> DeleteUserAsync(Guid currentUserId, Guid id);
}
