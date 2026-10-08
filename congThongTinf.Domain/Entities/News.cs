namespace congThongTinf.Domain.Entities;

/// <summary>
/// Represents a news article in the system
/// </summary>
public class News : BaseEntity
{
    public string Title { get; set; } = string.Empty;

    public string Summary { get; set; } = string.Empty;

    public string Content { get; set; } = string.Empty;

    public string? CoverImageUrl { get; set; }

    public string Category { get; set; } = string.Empty;

    public Guid AuthorId { get; set; }

    public User? Author { get; set; }

    public NewsStatus Status { get; set; } = NewsStatus.Draft;

    public DateTime? PublishedAt { get; set; }

    public int ViewCount { get; set; } = 0;
}

/// <summary>
/// Represents the possible statuses for a news article
/// </summary>
public enum NewsStatus
{
    Draft = 0,
    Pending = 1,
    Published = 2,
    Archived = 3
}
