using congThongTinf.Infrastructure.Commons;
using congThongTinf.Service.DTOs;

namespace congThongTinf.Service.Interfaces;

public interface IBannerService
{
    Task<Pagination<BannerDto>> GetAllBannersAsync(BannerQueryParameter param);

    Task<BannerDto> GetBannerByIdAsync(Guid id);

    Task<BannerDto> UploadBannerAsync(Guid currentUserId, UploadBannerForm form, CancellationToken cancellationToken = default);

    Task<BannerDto> UpdateBannerAsync(Guid id, UpdateBannerForm form, CancellationToken cancellationToken = default);

    Task<BannerDto> SetBannerActiveAsync(Guid id);

    Task<bool> DeleteBannerAsync(Guid id, CancellationToken cancellationToken = default);
}
