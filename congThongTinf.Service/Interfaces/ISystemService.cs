using congThongTinf.Service.DTOs;

namespace congThongTinf.Service.Interfaces;

public interface ISystemService
{
    Task<SeedAllResultDto> SeedAllAsync(CancellationToken cancellationToken = default);

    Task<SeedUsersRolesResultDto> SeedUsersRolesAsync();

    Task<int> SeedLeadersAsync();

    Task<int> SeedBannersAsync(CancellationToken cancellationToken = default);

    Task<int> SeedNewsAsync(CancellationToken cancellationToken = default);

    Task ClearDatabaseAsync();
}
