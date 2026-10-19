namespace congThongTinf.Domain.Entities;

/// <summary>
/// A user-uploaded file that can be shared with other authenticated users via a token link.
/// </summary>
public class SharedFile : BaseEntity
{
    public string FileName { get; set; } = string.Empty;

    public string FileType { get; set; } = string.Empty;

    public long FileSize { get; set; }

    public string FileKey { get; set; } = string.Empty;

    public Guid UploadedByUserId { get; set; }

    public User? UploadedByUser { get; set; }

    /// <summary>
    /// Opaque share token. Null means the file is not currently shared.
    /// </summary>
    public string? ShareToken { get; set; }
}
