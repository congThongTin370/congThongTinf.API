using Amazon.S3;
using Amazon.S3.Model;
using congThongTinf.Service.DTOs;
using congThongTinf.Service.Interfaces.Commons;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using congThongTinf.Service.Utils;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Webp;
using SixLabors.ImageSharp.Processing;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace congThongTinf.Service.Services.Commons
{
    public class ObjectStorageOptions
    {
        public const string SectionName = "ObjectStorage";

        public string ServiceUrl { get; set; } = string.Empty;
        public string BucketName { get; set; } = string.Empty;
        public string PublicUrl { get; set; } = string.Empty;
        public string AccessKey { get; set; } = string.Empty;
        public string SecretKey { get; set; } = string.Empty;
        public string Region { get; set; } = "us-east-1";
        public long StorageQuotaBytes { get; set; } = 10L * 1024 * 1024 * 1024;
    }

    public class BlobService : congThongTinf.Service.Interfaces.Commons.IBlobService
    {
        private const long MaxFileSizeBytes = 10 * 1024 * 1024; // 10 MB
        private const int MaxImageEdgePixels = 8192;
        private const int WebpQuality = 82;

        private readonly IAmazonS3 _s3Client;
        private readonly ObjectStorageOptions _options;
        private readonly ILogger<BlobService> _logger;

        public BlobService(IAmazonS3 s3Client, IOptions<ObjectStorageOptions> options, ILogger<BlobService> logger)
        {
            _s3Client = s3Client;
            _options = options.Value;
            _logger = logger;
        }

        public async Task<UploadFileResultDto> UploadFileAsync(IFormFile file, string folder = "uploads", CancellationToken cancellationToken = default, string? objectFileName = null)
        {
            var sw = Stopwatch.StartNew();
            _logger.LogInformation("Upload requested. Folder: {Folder}, File: {FileName}, ContentType: {ContentType}, Size: {Size} bytes",
                folder, file?.FileName, file?.ContentType, file?.Length);

            // Validation
            if (file == null || file.Length == 0)
            {
                _logger.LogWarning("Upload rejected: no file provided.");
                throw CreateValidationException("File is required.");
            }

            if (file.Length > MaxFileSizeBytes)
            {
                _logger.LogWarning("Upload rejected: {FileName} is {Size} bytes, exceeds limit of {Limit} bytes.",
                    file.FileName, file.Length, MaxFileSizeBytes);
                throw CreateValidationException("File exceeds 10 MB limit.");
            }

            // Buffer into a seekable MemoryStream with known length so the SDK sends a
            // plain signed payload instead of a streaming/chunked one (S3-compatible gateways
            // such as SeaweedFS often lack AWS flexible-checksum / streaming SigV4 support).
            await using var source = file.OpenReadStream();
            using var buffer = new MemoryStream((int)file.Length);
            await source.CopyToAsync(buffer, cancellationToken);

            var payload = await CreatePayloadAsync(file, buffer, cancellationToken);
            var extension = payload.Extension;
            var contentType = payload.ContentType;
            var size = payload.Size;

            var key = BuildObjectKey(folder, objectFileName, extension);
            _logger.LogInformation("Uploading to bucket '{Bucket}' with key '{Key}'...", _options.BucketName, key);

            var request = new PutObjectRequest
            {
                BucketName = _options.BucketName,
                Key = key,
                InputStream = payload.Stream,
                ContentType = contentType,
                AutoCloseStream = false,
                // AWSSDK only allows DisablePayloadSigning over HTTPS.
                DisablePayloadSigning = IsHttpsEndpoint,
                DisableDefaultChecksumValidation = true
            };

            try
            {
                await _s3Client.PutObjectAsync(request, cancellationToken);
            }
            catch (Exception ex)
            {
                if (payload.OwnsStream)
                    await payload.Stream.DisposeAsync();

                _logger.LogError(ex, "Object storage upload failed for key '{Key}' after {ElapsedMs} ms.", key, sw.ElapsedMilliseconds);
                throw;
            }

            _logger.LogInformation("Upload succeeded in {ElapsedMs} ms. Key: {Key}", sw.ElapsedMilliseconds, key);

            if (payload.OwnsStream)
                await payload.Stream.DisposeAsync();

            return new UploadFileResultDto
            {
                Key = key,
                Url = GetPublicUrl(key),
                Size = size,
                ContentType = contentType
            };
        }

        public async Task DeleteFileAsync(string key, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                _logger.LogWarning("Delete rejected: key is empty.");
                throw CreateValidationException("Key is required.");
            }

            _logger.LogInformation("Deleting object '{Key}' from bucket '{Bucket}'...", key, _options.BucketName);

            try
            {
                await _s3Client.DeleteObjectAsync(_options.BucketName, key, cancellationToken);
                _logger.LogInformation("Deleted object '{Key}'.", key);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Object storage delete failed for key '{Key}'.", key);
                throw;
            }
        }

        public async Task<List<BlobObjectDto>> ListImagesAsync(string folder, CancellationToken cancellationToken = default)
        {
            var prefix = (folder ?? string.Empty).Trim().Trim('/');
            if (string.IsNullOrEmpty(prefix))
                throw CreateValidationException("Folder is required.");

            if (string.IsNullOrWhiteSpace(_options.BucketName))
                throw ErrorHelper.Internal("Chưa cấu hình tên bucket lưu trữ.");

            prefix += "/";
            _logger.LogInformation("Listing images in object storage prefix {Prefix}.", prefix);

            var images = new List<BlobObjectDto>();
            try
            {
                string? continuationToken = null;
                do
                {
                    var response = await _s3Client.ListObjectsV2Async(new ListObjectsV2Request
                    {
                        BucketName = _options.BucketName,
                        Prefix = prefix,
                        ContinuationToken = continuationToken
                    }, cancellationToken);

                    foreach (var obj in response.S3Objects ?? [])
                    {
                        if (string.IsNullOrWhiteSpace(obj.Key) || obj.Key.EndsWith('/'))
                            continue;
                        if (!ImageFiles.HasImageExtension(obj.Key))
                            continue;

                        images.Add(new BlobObjectDto
                        {
                            Key = obj.Key,
                            Url = GetPublicUrl(obj.Key),
                            Size = obj.Size ?? 0,
                            LastModified = obj.LastModified?.ToUniversalTime()
                        });
                    }

                    continuationToken = response.IsTruncated == true ? response.NextContinuationToken : null;
                }
                while (continuationToken != null);
            }
            catch (AmazonS3Exception ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                _logger.LogWarning(ex, "Object storage bucket {Bucket} was not found.", _options.BucketName);
                throw ErrorHelper.NotFound("Không tìm thấy bucket lưu trữ.");
            }
            catch (AmazonS3Exception ex)
            {
                _logger.LogError(ex, "Object storage list failed for prefix {Prefix}.", prefix);
                throw ErrorHelper.WithStatus(502, "Không liệt kê được ảnh trong folder lưu trữ.");
            }

            return images
                .OrderByDescending(image => image.LastModified)
                .ThenBy(image => image.Key, StringComparer.Ordinal)
                .ToList();
        }

        public string GetPublicUrl(string key)
        {
            return $"{_options.PublicUrl.TrimEnd('/')}/{key}";
        }

        public string GetKeyFromPublicUrl(string url)
        {
            if (string.IsNullOrWhiteSpace(url))
                throw CreateValidationException("Url is required.");

            var prefix = _options.PublicUrl.TrimEnd('/') + "/";
            if (url.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                return url[prefix.Length..];

            if (Uri.TryCreate(url, UriKind.Absolute, out var uri))
                return uri.AbsolutePath.TrimStart('/');

            throw CreateValidationException("Url is not a valid object storage public URL.");
        }

        public async Task<BlobDownloadResult> DownloadFileAsync(string key, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                _logger.LogWarning("Download rejected: key is empty.");
                throw CreateValidationException("Key is required.");
            }

            _logger.LogInformation("Downloading object '{Key}' from bucket '{Bucket}'...", key, _options.BucketName);

            try
            {
                var response = await _s3Client.GetObjectAsync(_options.BucketName, key, cancellationToken);
                return new BlobDownloadResult
                {
                    Stream = response.ResponseStream,
                    ContentType = string.IsNullOrWhiteSpace(response.Headers.ContentType)
                        ? "application/octet-stream"
                        : response.Headers.ContentType,
                    OnDispose = () =>
                    {
                        response.Dispose();
                        return ValueTask.CompletedTask;
                    }
                };
            }
            catch (AmazonS3Exception ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                _logger.LogWarning("Object storage object '{Key}' was not found.", key);
                var notFound = new InvalidOperationException("File not found.");
                notFound.Data["StatusCode"] = 404;
                throw notFound;
            }
        }

        private async Task<PreparedBlob> CreatePayloadAsync(IFormFile file, MemoryStream buffer, CancellationToken cancellationToken)
        {
            if (!ImageFiles.IsImage(file))
            {
                buffer.Position = 0;
                var contentType = string.IsNullOrWhiteSpace(file.ContentType)
                    ? "application/octet-stream"
                    : file.ContentType;
                return new PreparedBlob(buffer, Path.GetExtension(file.FileName), contentType, buffer.Length, OwnsStream: false);
            }

            buffer.Position = 0;
            ImageInfo info;
            try
            {
                info = await Image.IdentifyAsync(buffer, cancellationToken);
            }
            catch (Exception ex) when (ex is UnknownImageFormatException or InvalidImageContentException)
            {
                _logger.LogWarning(ex, "Upload rejected: {FileName} is not a readable image.", file.FileName);
                throw CreateValidationException("Image file is invalid.");
            }

            if (info.Width <= 0 || info.Height <= 0 || info.Metadata.DecodedImageFormat == null)
                throw CreateValidationException("Image file is invalid.");

            if (info.Width > MaxImageEdgePixels || info.Height > MaxImageEdgePixels)
                throw CreateValidationException($"Image dimensions exceed {MaxImageEdgePixels} pixels.");

            buffer.Position = 0;
            using var image = await Image.LoadAsync(buffer, cancellationToken);
            image.Mutate(x => x.AutoOrient());

            if (image.Width > MaxImageEdgePixels || image.Height > MaxImageEdgePixels)
                throw CreateValidationException($"Image dimensions exceed {MaxImageEdgePixels} pixels.");

            var webp = new MemoryStream();
            try
            {
                await image.SaveAsWebpAsync(webp, new WebpEncoder
                {
                    Quality = WebpQuality,
                    FileFormat = WebpFileFormatType.Lossy
                }, cancellationToken);
            }
            catch
            {
                await webp.DisposeAsync();
                throw;
            }

            webp.Position = 0;

            _logger.LogInformation(
                "Converted {FileName} ({Width}x{Height}) to WebP quality {Quality}. Original {Original} bytes, WebP {Webp} bytes.",
                file.FileName, image.Width, image.Height, WebpQuality, file.Length, webp.Length);

            return new PreparedBlob(webp, ".webp", "image/webp", webp.Length, OwnsStream: true);
        }

        private static string BuildObjectKey(string folder, string? objectFileName, string extension)
        {
            var prefix = folder.Trim().Trim('/');
            if (string.IsNullOrWhiteSpace(objectFileName))
                return $"{prefix}/{Guid.NewGuid():N}{extension}";

            var name = Path.GetFileName(objectFileName.Trim());
            if (string.IsNullOrWhiteSpace(name))
                return $"{prefix}/{Guid.NewGuid():N}{extension}";

            return $"{prefix}/{name}";
        }

        private bool IsHttpsEndpoint =>
            _options.ServiceUrl.StartsWith("https://", StringComparison.OrdinalIgnoreCase);

        private readonly record struct PreparedBlob(MemoryStream Stream, string Extension, string ContentType, long Size, bool OwnsStream);

        private static Exception CreateValidationException(string message)
        {
            var ex = new ArgumentException(message);
            ex.Data["StatusCode"] = 400;
            return ex;
        }
    }
}
