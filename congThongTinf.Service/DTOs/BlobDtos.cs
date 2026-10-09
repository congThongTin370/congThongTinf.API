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
