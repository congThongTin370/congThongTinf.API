namespace congThongTinf.Domain.Entities;

/// <summary>
/// Homepage / portal banner image uploaded by admin.
/// </summary>
public class Banner : BaseEntity
{
    public string Title { get; set; } = string.Empty;

    public string ImageUrl { get; set; } = string.Empty;

    public string? LinkUrl { get; set; }

    public string FileKey { get; set; } = string.Empty;

    public string FileName { get; set; } = string.Empty;

    public string FileType { get; set; } = string.Empty;

    public long FileSize { get; set; }

    public BannerPosition Position { get; set; } = BannerPosition.Header;

    public bool IsActive { get; set; } = true;

    public Guid UploadedByUserId { get; set; }

    public User? UploadedByUser { get; set; }
}

/// <summary>
/// Fixed layout slots where a banner can be shown.
/// </summary>
public enum BannerPosition
{
    Header = 0,
    HomeMid = 1
}
