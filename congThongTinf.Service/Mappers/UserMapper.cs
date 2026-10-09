using congThongTinf.Domain.Entities;
using congThongTinf.Service.DTOs;

namespace congThongTinf.Service.Mappers;

public static class UserMapper
{
    public static UserDto ToUserDto(User user)
    {
        return new UserDto
        {
            UserId = user.Id,
            Username = user.Username,
            FullName = user.FullName,
            Email = user.Email,
            RoleName = user.Role?.Name ?? default,
            IsActive = user.IsActive,
            CreatedAt = user.CreatedAt
        };
    }
}
