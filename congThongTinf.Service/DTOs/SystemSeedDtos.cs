namespace congThongTinf.Service.DTOs;

public class SeedUsersRolesResultDto
{
    public int RoleCount { get; set; }

    public int AdminCount { get; set; }

    public int JournalistCount { get; set; }
}

public class SeedAllResultDto : SeedUsersRolesResultDto
{
    public int NewsCount { get; set; }

    public int LeaderCount { get; set; }

    public int BannerCount { get; set; }
}
