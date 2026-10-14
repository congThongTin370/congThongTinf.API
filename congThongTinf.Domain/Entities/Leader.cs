namespace congThongTinf.Domain.Entities;

/// <summary>
/// Unit leader shown on the portal leadership strip.
/// </summary>
public class Leader : BaseEntity
{
    public string FullName { get; set; } = string.Empty;

    public string Position { get; set; } = string.Empty;

    public string Rank { get; set; } = string.Empty;

    public string? ImageUrl { get; set; }

    public int DisplayOrder { get; set; }
}
