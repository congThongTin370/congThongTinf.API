namespace congThongTinf.Domain.Entities;

/// <summary>
/// Represents an internal division document
/// </summary>
public class Document : BaseEntity
{
    public string Title { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public string FileUrl { get; set; } = string.Empty;

    public string FileName { get; set; } = string.Empty;

    public string FileType { get; set; } = string.Empty;

    public long FileSize { get; set; }

    public Guid UploadedByUserId { get; set; }

    public User? UploadedByUser { get; set; }

    public DocumentCategory Category { get; set; } = DocumentCategory.General;
}

/// <summary>
/// Categories for internal documents
/// </summary>
public enum DocumentCategory
{
    General = 0,
    Policy = 1,
    Procedure = 2,
    Report = 3,
    Guideline = 4
}
