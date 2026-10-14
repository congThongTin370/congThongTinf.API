namespace congThongTinf.Service.DTOs;

public class LeaderDto
{
    public Guid Id { get; set; }

    public string FullName { get; set; } = string.Empty;

    public string Position { get; set; } = string.Empty;

    public string Rank { get; set; } = string.Empty;

    public string? ImageUrl { get; set; }

    public int DisplayOrder { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }
}

public class CreateLeaderDto
{
    public string FullName { get; set; } = string.Empty;

    public string Position { get; set; } = string.Empty;

    public string Rank { get; set; } = string.Empty;

    public string? ImageUrl { get; set; }

    public int DisplayOrder { get; set; }
}

public class UpdateLeaderDto
{
    public string? FullName { get; set; }

    public string? Position { get; set; }

    public string? Rank { get; set; }

    public string? ImageUrl { get; set; }

    public int? DisplayOrder { get; set; }
}
