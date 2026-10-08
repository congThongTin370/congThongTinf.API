using congThongTinf.Domain.Entities;
using System.ComponentModel;

namespace congThongTinf.Service.DTOs;

public class CreateJournalistUserDto
{
    public string Username { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string Password { get; set; } = string.Empty;

    public string FullName { get; set; } = string.Empty;
}

public class LoginRequestDto
{
    [DefaultValue("admin@370.local")]
    public string Email { get; set; } = string.Empty;
    [DefaultValue("Admin@123")]
    public string Password { get; set; } = string.Empty;
}

public class LoginResponseDto
{
    public string AccessToken { get; set; } = string.Empty;

    public UserDto? User { get; set; }
}

public class UserDto
{
    public Guid UserId { get; set; }

    public string Username { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string FullName { get; set; } = string.Empty;

    public RoleType RoleName { get; set; }

    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; }
}
