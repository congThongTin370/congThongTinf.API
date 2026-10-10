using Amazon.S3;
using Amazon.S3.Model;
using congThongTinf.Service.DTOs;
using congThongTinf.Service.Interfaces.Commons;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace congThongTinf.Service.Services.Commons
{
    public class CloudflareR2Options
    {
        public const string SectionName = "CloudflareR2";

        public string ServiceUrl { get; set; } = string.Empty;
        public string BucketName { get; set; } = string.Empty;
        public string PublicUrl { get; set; } = string.Empty;
        public string AccessKey { get; set; } = string.Empty;
        public string SecretKey { get; set; } = string.Empty;
    }

    public class BlobService : congThongTinf.Service.Interfaces.Commons.IBlobService
    {
        private const long MaxFileSizeBytes = 10 * 1024 * 1024; // 10 MB

        private readonly IAmazonS3 _s3Client;
        private readonly CloudflareR2Options _options;
        private readonly ILogger<BlobService> _logger;

        public BlobService(IAmazonS3 s3Client, IOptions<CloudflareR2Options> options, ILogger<BlobService> logger)
        {
            _s3Client = s3Client;
            _options = options.Value;
            _logger = logger;
        }

        public async Task<UploadFileResultDto> UploadFileAsync(IFormFile file, string folder = "uploads", CancellationToken cancellationToken = default)
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

            // Build key and upload
            var extension = Path.GetExtension(file.FileName);
            var key = $"{folder.Trim('/')}/{Guid.NewGuid():N}{extension}";
            var contentType = string.IsNullOrWhiteSpace(file.ContentType) ? "application/octet-stream" : file.ContentType;

            _logger.LogInformation("Uploading to R2 bucket '{Bucket}' with key '{Key}'...", _options.BucketName, key);

            // Buffer into a seekable MemoryStream with known length so the SDK sends a
            // plain signed payload instead of a streaming/chunked one (unsupported by R2).
            await using var source = file.OpenReadStream();
            using var buffer = new MemoryStream((int)file.Length);
            await source.CopyToAsync(buffer, cancellationToken);
            buffer.Position = 0;

            var request = new PutObjectRequest
            {
                BucketName = _options.BucketName,
                Key = key,
                InputStream = buffer,
                ContentType = contentType,
                AutoCloseStream = false,
                // R2 does not support Streaming SigV4 (STREAMING-AWS4-HMAC-SHA256-PAYLOAD).
                // Both flags must be set per request for AWSSDK.S3 4.x.
                DisablePayloadSigning = true,
                DisableDefaultChecksumValidation = true
            };

            try
            {
                await _s3Client.PutObjectAsync(request, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "R2 upload failed for key '{Key}' after {ElapsedMs} ms.", key, sw.ElapsedMilliseconds);
                throw;
            }

            _logger.LogInformation("Upload succeeded in {ElapsedMs} ms. Key: {Key}", sw.ElapsedMilliseconds, key);

            return new UploadFileResultDto
            {
                Key = key,
                Url = GetPublicUrl(key),
                Size = file.Length,
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
                _logger.LogError(ex, "R2 delete failed for key '{Key}'.", key);
                throw;
            }
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

            throw CreateValidationException("Url is not a valid R2 public URL.");
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
                _logger.LogWarning("R2 object '{Key}' was not found.", key);
                var notFound = new InvalidOperationException("File not found.");
                notFound.Data["StatusCode"] = 404;
                throw notFound;
            }
        }

        private static Exception CreateValidationException(string message)
        {
            var ex = new ArgumentException(message);
            ex.Data["StatusCode"] = 400;
            return ex;
        }
    }
}
