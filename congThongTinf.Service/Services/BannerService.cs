using System.Text.RegularExpressions;
using congThongTinf.Domain.Entities;
using congThongTinf.Infrastructure.Commons;
using congThongTinf.Service.DTOs;
using congThongTinf.Service.Interfaces;
using congThongTinf.Service.Interfaces.Commons;
using congThongTinf.Service.Utils;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace congThongTinf.Service.Services;

public class BannerService : IBannerService
{
    private static readonly Regex BannerFileNameRegex = new(
        @"^(top|mid)-banner(\d+)?\.webp$",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private readonly IBlobService _blobService;
    private readonly ILoggerService _logger;
    private readonly IUnitOfWork _unitOfWork;

    public BannerService(IUnitOfWork unitOfWork, ILoggerService logger, IBlobService blobService)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
        _blobService = blobService;
    }

    public Task<List<BlobObjectDto>> ListBannerImagesAsync(CancellationToken cancellationToken = default)
    {
        return _blobService.ListImagesAsync("banners", cancellationToken);
    }

    public async Task<Pagination<BannerDto>> GetAllBannersAsync(BannerQueryParameter param)
    {
        param ??= new BannerQueryParameter();
        if (param.PageSize <= 0)
            param.PageSize = 10;

        _logger.Info($"[GetAllBanners] Page: {param.PageIndex}, Size: {param.PageSize}");

        var query = _unitOfWork.Banners.GetQueryable().AsNoTracking();

        if (!string.IsNullOrWhiteSpace(param.Search))
        {
            var keyword = param.Search.Trim().ToLower();
            query = query.Where(b => !string.IsNullOrEmpty(b.Title) && b.Title.ToLower().Contains(keyword));
        }

        if (param.Position.HasValue)
        {
            EnsureValidPosition(param.Position.Value);
            query = query.Where(b => b.Position == param.Position.Value);
        }

        if (param.IsActive.HasValue)
            query = query.Where(b => b.IsActive == param.IsActive.Value);

        query = query.OrderBy(b => b.Position).ThenByDescending(b => b.CreatedAt);

        var total = await query.CountAsync();
        if (total == 0)
            _logger.Info("Không tìm thấy banner nào.");

        List<Banner> banners;
        if (param.PageIndex == 0)
            banners = await query.ToListAsync();
        else
            banners = await query
                .Skip((param.PageIndex - 1) * param.PageSize)
                .Take(param.PageSize)
                .ToListAsync();

        return new Pagination<BannerDto>(banners.Select(ToBannerDto).ToList(), total, param.PageIndex, param.PageSize);
    }

    public async Task<BannerDto> GetBannerByIdAsync(Guid id)
    {
        _logger.Info($"[GetBannerById] Requests banner {id}.");

        var banner = await _unitOfWork.Banners.GetByIdAsync(id);
        if (banner == null)
        {
            _logger.Warn($"[GetBannerById] Banner {id} not found.");
            throw ErrorHelper.NotFound($"Banner với ID {id} không tồn tại.");
        }

        return ToBannerDto(banner);
    }

    public async Task<BannerDto> UploadBannerAsync(Guid currentUserId, UploadBannerForm form, CancellationToken cancellationToken = default)
    {
        _logger.Info($"[UploadBannerAsync] Admin {currentUserId} uploads web banner.");

        await EnsureActiveUserAsync(currentUserId);
        if (form == null)
            throw ErrorHelper.BadRequest("Dữ liệu không hợp lệ.");

        var file = ValidateImage(form.File);
        EnsureValidPosition(form.Position);

        var title = string.IsNullOrWhiteSpace(form.Title)
            ? Path.GetFileNameWithoutExtension(file.FileName)
            : form.Title.Trim();

        var objectFileName = await AllocateBannerFileNameAsync(form.Position, cancellationToken);
        var upload = await _blobService.UploadFileAsync(file, "banners", cancellationToken, objectFileName);

        var banner = new Banner
        {
            Title = title,
            ImageUrl = upload.Url,
            LinkUrl = NormalizeLinkUrl(form.LinkUrl),
            FileKey = upload.Key,
            FileName = objectFileName,
            FileType = Truncate(upload.ContentType, 128),
            FileSize = upload.Size,
            Position = form.Position,
            IsActive = form.IsActive,
            UploadedByUserId = currentUserId
        };

        try
        {
            if (banner.IsActive)
                await DeactivateOthersInPositionAsync(banner.Position, banner.Id);

            await _unitOfWork.Banners.AddAsync(banner);
            await _unitOfWork.SaveChangesAsync();
        }
        catch
        {
            await TryDeleteBlobAsync(upload.Key, cancellationToken);
            throw;
        }

        _logger.Success($"[UploadBannerAsync] Banner {banner.Id} uploaded by admin {currentUserId}.");
        return ToBannerDto(banner);
    }

    public async Task<BannerDto> UpdateBannerAsync(Guid id, UpdateBannerForm form, CancellationToken cancellationToken = default)
    {
        _logger.Info($"[UpdateBanner] Starts updating banner {id}.");

        if (form == null)
            throw ErrorHelper.BadRequest("Dữ liệu không hợp lệ.");

        var banner = await _unitOfWork.Banners.GetByIdAsync(id);
        if (banner == null)
        {
            _logger.Warn($"[UpdateBanner] Banner {id} not found.");
            throw ErrorHelper.NotFound($"Banner với ID {id} không tồn tại.");
        }

        if (form.Title != null)
        {
            if (string.IsNullOrWhiteSpace(form.Title))
                throw ErrorHelper.BadRequest("Tiêu đề banner là bắt buộc.");

            banner.Title = form.Title.Trim();
        }

        if (form.LinkUrl != null)
            banner.LinkUrl = NormalizeLinkUrl(form.LinkUrl);

        var previousPosition = banner.Position;
        if (form.Position.HasValue)
        {
            EnsureValidPosition(form.Position.Value);
            banner.Position = form.Position.Value;
        }

        if (form.IsActive.HasValue)
            banner.IsActive = form.IsActive.Value;

        string? uploadedKey = null;
        var previousKey = banner.FileKey;
        if (form.File != null)
        {
            var file = ValidateImage(form.File);
            var reuseKey = banner.Position == previousPosition && LooksLikeBannerFileName(banner.FileName);
            var objectFileName = reuseKey
                ? Path.GetFileName(banner.FileName)
                : await AllocateBannerFileNameAsync(banner.Position, cancellationToken);
            var upload = await _blobService.UploadFileAsync(file, "banners", cancellationToken, objectFileName);
            uploadedKey = upload.Key;
            banner.ImageUrl = upload.Url;
            banner.FileKey = upload.Key;
            banner.FileName = objectFileName;
            banner.FileType = Truncate(upload.ContentType, 128);
            banner.FileSize = upload.Size;
        }

        try
        {
            if (banner.IsActive)
                await DeactivateOthersInPositionAsync(banner.Position, banner.Id);

            await _unitOfWork.Banners.Update(banner);
            await _unitOfWork.SaveChangesAsync();
        }
        catch
        {
            if (!string.IsNullOrWhiteSpace(uploadedKey))
                await TryDeleteBlobAsync(uploadedKey, cancellationToken);
            throw;
        }

        if (!string.IsNullOrWhiteSpace(uploadedKey)
            && !string.IsNullOrWhiteSpace(previousKey)
            && !string.Equals(previousKey, uploadedKey, StringComparison.Ordinal))
            await TryDeleteBlobAsync(previousKey, cancellationToken);

        _logger.Success($"[UpdateBanner] Banner {id} updated.");
        return ToBannerDto(banner);
    }

    public async Task<BannerDto> SetBannerActiveAsync(Guid id)
    {
        _logger.Info($"[SetBannerActive] Sets banner {id} as the active banner for its position.");

        var banner = await _unitOfWork.Banners.GetByIdAsync(id);
        if (banner == null)
        {
            _logger.Warn($"[SetBannerActive] Banner {id} not found.");
            throw ErrorHelper.NotFound($"Banner với ID {id} không tồn tại.");
        }

        await DeactivateOthersInPositionAsync(banner.Position, banner.Id);

        banner.IsActive = true;
        await _unitOfWork.Banners.Update(banner);
        await _unitOfWork.SaveChangesAsync();

        _logger.Success($"[SetBannerActive] Banner {id} is now active for position {banner.Position}.");
        return ToBannerDto(banner);
    }

    public async Task<bool> DeleteBannerAsync(Guid id, CancellationToken cancellationToken = default)
    {
        _logger.Info($"[DeleteBanner] Starts deleting banner {id}.");

        var banner = await _unitOfWork.Banners.GetByIdAsync(id);
        if (banner == null)
        {
            _logger.Warn($"[DeleteBanner] Banner {id} not found.");
            throw ErrorHelper.NotFound($"Banner với ID {id} không tồn tại.");
        }

        var fileKey = banner.FileKey;
        await _unitOfWork.Banners.HardRemoveRange(new List<Banner> { banner });
        await _unitOfWork.SaveChangesAsync();

        if (!string.IsNullOrWhiteSpace(fileKey))
            await TryDeleteBlobAsync(fileKey, cancellationToken);

        _logger.Success($"[DeleteBanner] Banner {id} deleted.");
        return true;
    }

    private async Task DeactivateOthersInPositionAsync(BannerPosition position, Guid exceptId)
    {
        var others = await _unitOfWork.Banners.GetQueryable()
            .Where(b => b.IsActive && b.Position == position && b.Id != exceptId)
            .ToListAsync();

        foreach (var other in others)
            other.IsActive = false;
    }

    private static void EnsureValidPosition(BannerPosition position)
    {
        if (!Enum.IsDefined(position))
            throw ErrorHelper.BadRequest("Vị trí banner không hợp lệ.");
    }

    private async Task<string> AllocateBannerFileNameAsync(BannerPosition position, CancellationToken cancellationToken)
    {
        var prefix = ToBannerFilePrefix(position);
        var images = await _blobService.ListImagesAsync("banners", cancellationToken);
        var maxNumber = 0;

        foreach (var image in images)
        {
            var name = Path.GetFileName(image.Key);
            if (!TryParseBannerNumber(name, prefix, out var number))
                continue;

            if (number > maxNumber)
                maxNumber = number;
        }

        return $"{prefix}-banner{maxNumber + 1}.webp";
    }

    private static string ToBannerFilePrefix(BannerPosition position)
    {
        return position switch
        {
            BannerPosition.Header => "top",
            BannerPosition.HomeMid => "mid",
            _ => throw ErrorHelper.BadRequest("Vị trí banner không hợp lệ.")
        };
    }

    private static bool LooksLikeBannerFileName(string? fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
            return false;

        return BannerFileNameRegex.IsMatch(Path.GetFileName(fileName));
    }

    private static bool TryParseBannerNumber(string fileName, string prefix, out int number)
    {
        number = 0;
        var match = BannerFileNameRegex.Match(fileName);
        if (!match.Success)
            return false;

        if (!string.Equals(match.Groups[1].Value, prefix, StringComparison.OrdinalIgnoreCase))
            return false;

        number = match.Groups[2].Success && match.Groups[2].Length > 0
            ? int.Parse(match.Groups[2].Value)
            : 1;
        return true;
    }

    private static string? NormalizeLinkUrl(string? linkUrl)
    {
        if (string.IsNullOrWhiteSpace(linkUrl))
            return null;

        var trimmed = linkUrl.Trim();
        if (trimmed.Length > 1000)
            throw ErrorHelper.BadRequest("Link banner không được vượt quá 1000 ký tự.");

        return trimmed;
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

    private static IFormFile ValidateImage(IFormFile? file)
    {
        if (file == null || file.Length == 0)
            throw ErrorHelper.BadRequest("File là bắt buộc.");

        if (!ImageFiles.IsImage(file))
            throw ErrorHelper.BadRequest("Banner phải là ảnh.");

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
            _logger.Warn($"[TryDeleteBlobAsync] Failed to delete blob '{key}': {ex.Message}");
        }
    }

    private static string Truncate(string value, int maxLength)
    {
        if (string.IsNullOrEmpty(value) || value.Length <= maxLength)
            return value;

        return value[..maxLength];
    }

    private static BannerDto ToBannerDto(Banner banner)
    {
        return new BannerDto
        {
            Id = banner.Id,
            Title = banner.Title,
            ImageUrl = banner.ImageUrl,
            LinkUrl = banner.LinkUrl,
            FileName = banner.FileName,
            FileType = banner.FileType,
            FileSize = banner.FileSize,
            Position = banner.Position,
            IsActive = banner.IsActive,
            UploadedByUserId = banner.UploadedByUserId,
            CreatedAt = banner.CreatedAt
        };
    }
}
