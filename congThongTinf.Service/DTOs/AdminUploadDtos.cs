using congThongTinf.Domain.Entities;
using Microsoft.AspNetCore.Http;

namespace congThongTinf.Service.DTOs;

public class UploadBannerForm
{
    public IFormFile File { get; set; } = default!;

    public string? Title { get; set; }

    public int DisplayOrder { get; set; }

    public bool IsActive { get; set; } = true;
}

public class BannerDto
{
    public Guid Id { get; set; }

    public string Title { get; set; } = string.Empty;

    public string ImageUrl { get; set; } = string.Empty;

    public string FileName { get; set; } = string.Empty;

    public string FileType { get; set; } = string.Empty;

    public long FileSize { get; set; }

    public int DisplayOrder { get; set; }

    public bool IsActive { get; set; }

    public Guid UploadedByUserId { get; set; }

    public DateTime CreatedAt { get; set; }
}

public class UploadDocumentForm
{
    public IFormFile File { get; set; } = default!;

    public string Title { get; set; } = string.Empty;

    public string? Description { get; set; }

    public DocumentCategory Category { get; set; } = DocumentCategory.General;
}

public class DocumentDto
{
    public Guid Id { get; set; }

    public string Title { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public string FileUrl { get; set; } = string.Empty;

    public string DownloadUrl { get; set; } = string.Empty;

    public string FileName { get; set; } = string.Empty;

    public string FileType { get; set; } = string.Empty;

    public long FileSize { get; set; }

    public DocumentCategory Category { get; set; }

    public Guid UploadedByUserId { get; set; }

    public string? UploadedByUserName { get; set; }

    public DateTime CreatedAt { get; set; }
}

public class DocumentQueryParameter
{
    public int PageIndex { get; set; } = 1;

    public int PageSize { get; set; } = 10;

    public string? Search { get; set; }

    public DocumentCategory? Category { get; set; }
}
