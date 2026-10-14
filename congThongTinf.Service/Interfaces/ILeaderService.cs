using congThongTinf.Service.DTOs;

namespace congThongTinf.Service.Interfaces;

public interface ILeaderService
{
    Task<List<LeaderDto>> GetAllLeadersAsync();

    Task<LeaderDto> GetLeaderByIdAsync(Guid id);

    Task<LeaderDto> CreateLeaderAsync(CreateLeaderDto dto);

    Task<LeaderDto> UpdateLeaderAsync(Guid id, UpdateLeaderDto dto);

    Task<bool> DeleteLeaderAsync(Guid id);
}
