using congThongTinf.Service.DTOs;
using Microsoft.Extensions.Configuration;

namespace congThongTinf.Service.Interfaces;

public interface IAuthService
{
    Task<UserDto> CreateJournalistUser(CreateJournalistUserDto dto, Guid adminUserId);

    Task<LoginResponseDto> LoginAsync(LoginRequestDto loginDto, IConfiguration configuration);

    Task<bool> LogoutAsync(Guid userId);
}
