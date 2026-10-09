using congThongTinf.Domain.Entities;
using congThongTinf.Infrastructure.Commons;
using congThongTinf.Service.DTOs;
using congThongTinf.Service.Interfaces;
using congThongTinf.Service.Interfaces.Commons;
using congThongTinf.Service.Utils;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace congThongTinf.Service.Services;

public class DocumentService : IDocumentService
{
    private static readonly HashSet<string> DocumentExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".pdf", ".doc", ".docx", ".xls", ".xlsx", ".ppt", ".pptx", ".txt"
    };

    private readonly IBlobService _blobService;
    private readonly ILoggerService _logger;
    private readonly IUnitOfWork _unitOfWork;

    public DocumentService(
        IUnitOfWork unitOfWork,
        ILoggerService logger,
        IBlobService blobService)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
        _blobService = blobService;
    }

    public async Task<Pagination<DocumentDto>> GetAllDocumentsAsync(DocumentQueryParameter param)
    {
        param ??= new DocumentQueryParameter();
        if (param.PageSize <= 0)
            param.PageSize = 10;

        _logger.Info($"[GetAllDocumentsAsync] Page: {param.PageIndex}, Size: {param.PageSize}");

        var query = _unitOfWork.Documents.GetQueryable()
            .Include(d => d.UploadedByUser)
            .AsNoTracking();

        if (!string.IsNullOrWhiteSpace(param.Search))
        {
            var keyword = param.Search.Trim().ToLower();
            query = query.Where(d =>
                (!string.IsNullOrEmpty(d.Title) && d.Title.ToLower().Contains(keyword)) ||
                (!string.IsNullOrEmpty(d.FileName) && d.FileName.ToLower().Contains(keyword)) ||
                (!string.IsNullOrEmpty(d.Description) && d.Description.ToLower().Contains(keyword)));
        }

        if (param.Category.HasValue)
        {
            if (!Enum.IsDefined(param.Category.Value))
                throw ErrorHelper.BadRequest("Danh mục tài liệu không hợp lệ.");

            query = query.Where(d => d.Category == param.Category.Value);
        }

        query = query.OrderByDescending(d => d.CreatedAt);

        var total = await query.CountAsync();
        if (total == 0)
            _logger.Info("Không tìm thấy tài liệu nội bộ nào.");

        List<Document> documents;
        if (param.PageIndex == 0)
            documents = await query.ToListAsync();
        else
            documents = await query
                .Skip((param.PageIndex - 1) * param.PageSize)
                .Take(param.PageSize)
                .ToListAsync();

        var dtos = documents.Select(ToDocumentDto).ToList();
        return new Pagination<DocumentDto>(dtos, total, param.PageIndex, param.PageSize);
    }

    public async Task<DocumentDto> UploadInternalDocumentAsync(Guid currentUserId, UploadDocumentForm form, CancellationToken cancellationToken = default)
    {
        _logger.Info($"[UploadInternalDocumentAsync] Admin {currentUserId} uploads internal document.");

        await EnsureActiveUserAsync(currentUserId);
        if (form == null)
            throw ErrorHelper.BadRequest("Dữ liệu không hợp lệ.");

        var file = ValidateUploadForm(form.File);
        if (!Enum.IsDefined(form.Category))
            throw ErrorHelper.BadRequest("Danh mục tài liệu không hợp lệ.");

        var title = string.IsNullOrWhiteSpace(form.Title)
            ? Path.GetFileNameWithoutExtension(file.FileName)
            : form.Title.Trim();

        var upload = await _blobService.UploadFileAsync(file, "documents", cancellationToken);

        var document = new Document
        {
            Title = title,
            Description = form.Description?.Trim() ?? string.Empty,
            FileUrl = upload.Url,
            FileName = file.FileName,
            FileType = Truncate(upload.ContentType, 128),
            FileSize = upload.Size,
            UploadedByUserId = currentUserId,
            Category = form.Category
        };

        try
        {
            await _unitOfWork.Documents.AddAsync(document);
            await _unitOfWork.SaveChangesAsync();
        }
        catch
        {
            await TryDeleteBlobAsync(upload.Key, cancellationToken);
            throw;
        }

        _logger.Success($"[UploadInternalDocumentAsync] Document {document.Id} uploaded by admin {currentUserId}.");
        return ToDocumentDto(document);
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

    private static IFormFile ValidateUploadForm(IFormFile? file)
    {
        if (file == null || file.Length == 0)
            throw ErrorHelper.BadRequest("File là bắt buộc.");

        var extension = Path.GetExtension(file.FileName);
        if (string.IsNullOrWhiteSpace(extension) || !DocumentExtensions.Contains(extension))
            throw ErrorHelper.BadRequest("Tài liệu nội bộ phải là pdf, doc, docx, xls, xlsx, ppt, pptx hoặc txt.");

        return file;
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

    private static DocumentDto ToDocumentDto(Document document)
    {
        return new DocumentDto
        {
            Id = document.Id,
            Title = document.Title,
            Description = document.Description,
            FileUrl = document.FileUrl,
            FileName = document.FileName,
            FileType = document.FileType,
            FileSize = document.FileSize,
            Category = document.Category,
            UploadedByUserId = document.UploadedByUserId,
            UploadedByUserName = document.UploadedByUser?.FullName,
            CreatedAt = document.CreatedAt
        };
    }
}
