using congThongTinf.Infrastructure.Commons;
using congThongTinf.Service.DTOs;

namespace congThongTinf.Service.Interfaces;

public interface ISharedFileService
{
    Task<Pagination<SharedFileDto>> GetMyFilesAsync(Guid currentUserId, SharedFileQueryParameter param);

    Task<SharedFileDto> GetMyFileAsync(Guid currentUserId, Guid fileId);

    Task<SharedFileDto> UploadAsync(Guid currentUserId, UploadSharedFileForm form, CancellationToken cancellationToken = default);

    Task<SharedFileDto> CreateShareLinkAsync(Guid currentUserId, Guid fileId);

    Task<SharedFileDto> RevokeShareLinkAsync(Guid currentUserId, Guid fileId);

    Task<SharedFileDto> GetByShareTokenAsync(string token);

    Task<BlobDownloadResult> DownloadMyFileAsync(Guid currentUserId, Guid fileId, CancellationToken cancellationToken = default);

    Task<BlobDownloadResult> OpenByShareTokenAsync(string token, bool asAttachment, CancellationToken cancellationToken = default);
}
