namespace congThongTinf.Repository.Entities;

/// <summary>
/// Represents a news category/classification
/// </summary>
public class Category : BaseEntity
{
    public string Name { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public string? Icon { get; set; }

    public int DisplayOrder { get; set; } = 0;

    public bool IsActive { get; set; } = true;

    // Navigation properties
    public ICollection<News> Articles { get; set; } = new List<News>();
}
