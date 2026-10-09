using congThongTinf.Domain.Entities;

namespace congThongTinf.Service.DTOs;

public class UserCreateDto
{
    public string Username { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string Password { get; set; } = string.Empty;

    public string FullName { get; set; } = string.Empty;

    public RoleType RoleName { get; set; }
}

public class UpdateProfileDto
{
    public string? FullName { get; set; }
}

public class UserQueryParameter
{
    public int PageIndex { get; set; } = 1;

    public int PageSize { get; set; } = 10;

    public string? Search { get; set; }

    public bool? IsActive { get; set; }

    public RoleType? RoleName { get; set; }

    public bool Desc { get; set; } = true;
}
