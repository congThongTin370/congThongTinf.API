using Microsoft.AspNetCore.Http;

namespace congThongTinf.Service.Utils;

public static class ImageFiles
{
    private static readonly HashSet<string> Extensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".jpg", ".jpeg", ".jfif", ".png", ".gif", ".webp", ".bmp",
        ".tif", ".tiff", ".tga", ".pbm", ".pgm", ".ppm", ".qoi"
    };

    public static bool IsImage(IFormFile file)
    {
        if (!string.IsNullOrWhiteSpace(file.ContentType)
            && file.ContentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
            return true;

        var extension = Path.GetExtension(file.FileName);
        return !string.IsNullOrWhiteSpace(extension) && Extensions.Contains(extension);
    }

    public static bool HasImageExtension(string? key)
    {
        var extension = Path.GetExtension(key);
        return !string.IsNullOrWhiteSpace(extension) && Extensions.Contains(extension);
    }
}
