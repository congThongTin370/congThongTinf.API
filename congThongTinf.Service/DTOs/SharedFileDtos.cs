using Microsoft.AspNetCore.Http;

namespace congThongTinf.Service.DTOs;

public class UploadSharedFileForm
{
    public IFormFile File { get; set; } = default!;
}

public class SharedFileQueryParameter
{
    public int PageIndex { get; set; } = 1;

    public int PageSize { get; set; } = 10;

    public string? Search { get; set; }
}

public class SharedFileDto
{
    public Guid Id { get; set; }

    public string FileName { get; set; } = string.Empty;

    public string FileType { get; set; } = string.Empty;

    public long FileSize { get; set; }

    public Guid UploadedByUserId { get; set; }

    public string? UploadedByUserName { get; set; }

    public DateTime CreatedAt { get; set; }

    public bool IsShared { get; set; }

    public string? ShareToken { get; set; }

    public string? ShareUrl { get; set; }

    public string ViewUrl { get; set; } = string.Empty;

    public string DownloadUrl { get; set; } = string.Empty;
}
