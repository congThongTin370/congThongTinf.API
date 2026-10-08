namespace congThongTinf.Repository.Entities;

/// <summary>
/// Represents a role in the system (Admin, Journalist, etc.)
/// </summary>
public class Role
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    // Navigation properties
    public ICollection<User> Users { get; set; } = new List<User>();
}
