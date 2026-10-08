namespace congThongTinf.Domain.Entities;

/// <summary>
/// Represents a role in the system (Admin, Journalist, etc.)
/// </summary>
public class Role : BaseEntity
{
    public RoleType Name { get; set; }

    public string Description { get; set; } = string.Empty;

    // Navigation properties
    public ICollection<User> Users { get; set; } = new List<User>();
}
