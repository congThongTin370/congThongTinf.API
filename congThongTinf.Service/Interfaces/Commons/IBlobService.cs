using congThongTinf.Service.DTOs;
using Microsoft.AspNetCore.Http;
using System.Threading;
using System.Threading.Tasks;

namespace congThongTinf.Service.Interfaces.Commons
{
    public interface IBlobService
    {
        /// <summary>
        /// Validates and uploads a file to Cloudflare R2.
        /// </summary>
        Task<UploadFileResultDto> UploadFileAsync(IFormFile file, string folder = "uploads", CancellationToken cancellationToken = default);

        /// <summary>
        /// Deletes an object from Cloudflare R2.
        /// </summary>
        Task DeleteFileAsync(string key, CancellationToken cancellationToken = default);

        /// <summary>
        /// Returns a publicly accessible URL for an object key.
        /// </summary>
        string GetPublicUrl(string key);

        /// <summary>
        /// Opens an object stream from R2. Caller must dispose the result.
        /// </summary>
        Task<BlobDownloadResult> DownloadFileAsync(string key, CancellationToken cancellationToken = default);

        /// <summary>
        /// Maps a public object URL back to the storage key.
        /// </summary>
        string GetKeyFromPublicUrl(string url);

        /// <summary>
        /// Lists image objects under a folder prefix in the configured bucket.
        /// </summary>
        Task<List<BlobObjectDto>> ListImagesAsync(string folder, CancellationToken cancellationToken = default);
    }
}
