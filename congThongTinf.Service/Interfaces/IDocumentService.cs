using congThongTinf.Infrastructure.Commons;
using congThongTinf.Service.DTOs;

namespace congThongTinf.Service.Interfaces;

public interface IDocumentService
{
    Task<Pagination<DocumentDto>> GetAllDocumentsAsync(DocumentQueryParameter param);
    Task<DocumentDto> UploadInternalDocumentAsync(Guid currentUserId, UploadDocumentForm form, CancellationToken cancellationToken = default);
    Task<BlobDownloadResult> DownloadDocumentAsync(Guid documentId, CancellationToken cancellationToken = default);
}
