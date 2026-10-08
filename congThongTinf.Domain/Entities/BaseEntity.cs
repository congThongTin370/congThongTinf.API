namespace congThongTinf.Domain.Entities;

/// <summary>
/// Base class for all entities in the domain
/// </summary>
public abstract class BaseEntity
{
    public Guid Id { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }
}
