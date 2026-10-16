using Microsoft.AspNetCore.Http;

namespace congThongTinf.Service.DTOs;

public class UploadFileForm
{
    public IFormFile File { get; set; } = default!;
}

public class UploadFileResultDto
{
    public string Key { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public long Size { get; set; }
    public string ContentType { get; set; } = string.Empty;
}

public class BlobObjectDto
{
    public string Key { get; set; } = string.Empty;

    public string Url { get; set; } = string.Empty;

    public long Size { get; set; }

    public DateTime? LastModified { get; set; }
}

public class BlobDownloadResult : IAsyncDisposable
{
    public required Stream Stream { get; init; }

    public string ContentType { get; set; } = "application/octet-stream";

    public string FileName { get; set; } = string.Empty;

    public Func<ValueTask>? OnDispose { get; init; }

    public async ValueTask DisposeAsync()
    {
        if (OnDispose != null)
            await OnDispose();
        else
            await Stream.DisposeAsync();
    }
}
