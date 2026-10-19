using congThongTinf.Domain.Entities;
using congThongTinf.Infrastructure.Commons;
using congThongTinf.Service.DTOs;
using congThongTinf.Service.Interfaces;
using congThongTinf.Service.Interfaces.Commons;
using congThongTinf.Service.Utils;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace congThongTinf.Service.Services;

public class SharedFileService : ISharedFileService
{
    private readonly IBlobService _blobService;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILoggerService _logger;
    private readonly IUnitOfWork _unitOfWork;

    public SharedFileService(
        IUnitOfWork unitOfWork,
        ILoggerService logger,
        IBlobService blobService,
        IHttpContextAccessor httpContextAccessor)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
        _blobService = blobService;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<Pagination<SharedFileDto>> GetMyFilesAsync(Guid currentUserId, SharedFileQueryParameter param)
    {
        param ??= new SharedFileQueryParameter();
        if (param.PageSize <= 0)
            param.PageSize = 10;

        await EnsureActiveUserAsync(currentUserId);
        _logger.Info($"[GetMyFilesAsync] User {currentUserId}. Page: {param.PageIndex}, Size: {param.PageSize}");

        var query = _unitOfWork.SharedFiles.GetQueryable()
            .Include(f => f.UploadedByUser)
            .AsNoTracking()
            .Where(f => f.UploadedByUserId == currentUserId);

        if (!string.IsNullOrWhiteSpace(param.Search))
        {
            var keyword = param.Search.Trim().ToLower();
            query = query.Where(f => !string.IsNullOrEmpty(f.FileName) && f.FileName.ToLower().Contains(keyword));
        }

        query = query.OrderByDescending(f => f.CreatedAt);

        var total = await query.CountAsync();

        List<SharedFile> files;
        if (param.PageIndex == 0)
            files = await query.ToListAsync();
        else
            files = await query
                .Skip((param.PageIndex - 1) * param.PageSize)
                .Take(param.PageSize)
                .ToListAsync();

        var dtos = files.Select(f => ToDto(f, forOwner: true)).ToList();
        return new Pagination<SharedFileDto>(dtos, total, param.PageIndex, param.PageSize);
    }

    public async Task<SharedFileDto> GetMyFileAsync(Guid currentUserId, Guid fileId)
    {
        await EnsureActiveUserAsync(currentUserId);
        var file = await GetOwnedFileAsync(currentUserId, fileId, tracking: false);
        return ToDto(file, forOwner: true);
    }

    public async Task<SharedFileDto> UploadAsync(Guid currentUserId, UploadSharedFileForm form, CancellationToken cancellationToken = default)
    {
        _logger.Info($"[UploadAsync] User {currentUserId} uploads a shared file.");

        await EnsureActiveUserAsync(currentUserId);
        if (form == null)
            throw ErrorHelper.BadRequest("Dữ liệu không hợp lệ.");

        var file = form.File;
        if (file == null || file.Length == 0)
            throw ErrorHelper.BadRequest("File là bắt buộc.");

        var upload = await _blobService.UploadFileAsync(file, $"shared/{currentUserId}", cancellationToken);

        var sharedFile = new SharedFile
        {
            FileName = Truncate(file.FileName, 256),
            FileType = Truncate(upload.ContentType, 128),
            FileSize = upload.Size,
            FileKey = upload.Key,
            UploadedByUserId = currentUserId
        };

        try
        {
            await _unitOfWork.SharedFiles.AddAsync(sharedFile);
            await _unitOfWork.SaveChangesAsync();
        }
        catch
        {
            await TryDeleteBlobAsync(upload.Key, cancellationToken);
            throw;
        }

        _logger.Success($"[UploadAsync] Shared file {sharedFile.Id} uploaded by {currentUserId}.");
        return ToDto(sharedFile, forOwner: true);
    }

    public async Task<SharedFileDto> CreateShareLinkAsync(Guid currentUserId, Guid fileId)
    {
        await EnsureActiveUserAsync(currentUserId);
        var file = await GetOwnedFileAsync(currentUserId, fileId, tracking: true);

        if (string.IsNullOrEmpty(file.ShareToken))
        {
            file.ShareToken = Guid.NewGuid().ToString("N");
            await _unitOfWork.SharedFiles.Update(file);
            await _unitOfWork.SaveChangesAsync();
            _logger.Success($"[CreateShareLinkAsync] Created share token for file {file.Id}.");
        }

        return ToDto(file, forOwner: true);
    }

    public async Task<SharedFileDto> RevokeShareLinkAsync(Guid currentUserId, Guid fileId)
    {
        await EnsureActiveUserAsync(currentUserId);
        var file = await GetOwnedFileAsync(currentUserId, fileId, tracking: true);

        if (!string.IsNullOrEmpty(file.ShareToken))
        {
            file.ShareToken = null;
            await _unitOfWork.SharedFiles.Update(file);
            await _unitOfWork.SaveChangesAsync();
            _logger.Success($"[RevokeShareLinkAsync] Revoked share token for file {file.Id}.");
        }

        return ToDto(file, forOwner: true);
    }

    public async Task<SharedFileDto> GetByShareTokenAsync(string token)
    {
        var file = await GetSharedFileByTokenAsync(token, tracking: false);
        return ToDto(file, forOwner: false);
    }

    public async Task<BlobDownloadResult> DownloadMyFileAsync(Guid currentUserId, Guid fileId, CancellationToken cancellationToken = default)
    {
        await EnsureActiveUserAsync(currentUserId);
        var file = await GetOwnedFileAsync(currentUserId, fileId, tracking: false);
        return await OpenFileAsync(file, cancellationToken);
    }

    public async Task<BlobDownloadResult> OpenByShareTokenAsync(string token, bool asAttachment, CancellationToken cancellationToken = default)
    {
        var file = await GetSharedFileByTokenAsync(token, tracking: false);
        var download = await OpenFileAsync(file, cancellationToken);
        if (!asAttachment)
            download.FileName = string.Empty;
        return download;
    }

    private async Task<SharedFile> GetOwnedFileAsync(Guid currentUserId, Guid fileId, bool tracking)
    {
        IQueryable<SharedFile> query = _unitOfWork.SharedFiles.GetQueryable()
            .Include(f => f.UploadedByUser);
        if (!tracking)
            query = query.AsNoTracking();

        var file = await query.FirstOrDefaultAsync(f => f.Id == fileId);
        if (file == null)
            throw ErrorHelper.NotFound($"File với ID {fileId} không tồn tại.");

        if (file.UploadedByUserId != currentUserId)
            throw ErrorHelper.Forbidden("Bạn không có quyền thao tác file này.");

        return file;
    }

    private async Task<SharedFile> GetSharedFileByTokenAsync(string token, bool tracking)
    {
        if (string.IsNullOrWhiteSpace(token))
            throw ErrorHelper.BadRequest("Liên kết chia sẻ không hợp lệ.");

        IQueryable<SharedFile> query = _unitOfWork.SharedFiles.GetQueryable()
            .Include(f => f.UploadedByUser);
        if (!tracking)
            query = query.AsNoTracking();

        var file = await query.FirstOrDefaultAsync(f => f.ShareToken == token.Trim());
        if (file == null)
            throw ErrorHelper.NotFound("Liên kết chia sẻ không hợp lệ hoặc đã bị thu hồi.");

        return file;
    }

    private async Task<BlobDownloadResult> OpenFileAsync(SharedFile file, CancellationToken cancellationToken)
    {
        var download = await _blobService.DownloadFileAsync(file.FileKey, cancellationToken);
        download.FileName = string.IsNullOrWhiteSpace(file.FileName)
            ? Path.GetFileName(file.FileKey)
            : file.FileName;

        if (string.IsNullOrWhiteSpace(download.ContentType) || download.ContentType == "application/octet-stream")
            download.ContentType = string.IsNullOrWhiteSpace(file.FileType)
                ? "application/octet-stream"
                : file.FileType;

        return download;
    }

    private async Task EnsureActiveUserAsync(Guid userId)
    {
        if (userId == Guid.Empty)
            throw ErrorHelper.Unauthorized("Không xác định được tài khoản hiện tại.");

        var user = await _unitOfWork.Users.FirstOrDefaultAsync(u => u.Id == userId && u.IsActive);
        if (user == null)
        {
            _logger.Warn($"[EnsureActiveUserAsync] User {userId} not found.");
            throw ErrorHelper.NotFound($"Người dùng với ID {userId} không tồn tại.");
        }
    }

    private async Task TryDeleteBlobAsync(string key, CancellationToken cancellationToken)
    {
        try
        {
            await _blobService.DeleteFileAsync(key, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.Warn($"[TryDeleteBlobAsync] Failed to roll back blob '{key}': {ex.Message}");
        }
    }

    private static string Truncate(string value, int maxLength)
    {
        if (string.IsNullOrEmpty(value) || value.Length <= maxLength)
            return value;

        return value[..maxLength];
    }

    private SharedFileDto ToDto(SharedFile file, bool forOwner)
    {
        var isShared = !string.IsNullOrEmpty(file.ShareToken);
        var dto = new SharedFileDto
        {
            Id = file.Id,
            FileName = file.FileName,
            FileType = file.FileType,
            FileSize = file.FileSize,
            UploadedByUserId = file.UploadedByUserId,
            UploadedByUserName = file.UploadedByUser?.FullName,
            CreatedAt = file.CreatedAt,
            IsShared = isShared,
            ShareToken = forOwner && isShared ? file.ShareToken : null,
            ShareUrl = forOwner && isShared ? BuildAbsoluteUrl($"/api/shared-files/links/{file.ShareToken}") : null
        };

        if (forOwner)
        {
            dto.ViewUrl = BuildAbsoluteUrl($"/api/shared-files/{file.Id}/view");
            dto.DownloadUrl = BuildAbsoluteUrl($"/api/shared-files/{file.Id}/download");
        }
        else
        {
            dto.ViewUrl = BuildAbsoluteUrl($"/api/shared-files/links/{file.ShareToken}/view");
            dto.DownloadUrl = BuildAbsoluteUrl($"/api/shared-files/links/{file.ShareToken}/download");
        }

        return dto;
    }

    private string BuildAbsoluteUrl(string relativePath)
    {
        var request = _httpContextAccessor.HttpContext?.Request;
        if (request == null)
            return relativePath;

        var pathBase = request.PathBase.HasValue ? request.PathBase.Value.TrimEnd('/') : string.Empty;
        return $"{request.Scheme}://{request.Host}{pathBase}{relativePath}";
    }
}
