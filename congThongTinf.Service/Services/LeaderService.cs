using congThongTinf.Domain.Entities;
using congThongTinf.Service.DTOs;
using congThongTinf.Service.Interfaces;
using congThongTinf.Service.Interfaces.Commons;
using congThongTinf.Service.Utils;
using Microsoft.EntityFrameworkCore;

namespace congThongTinf.Service.Services;

public class LeaderService : ILeaderService
{
    private const int FullNameMaxLength = 256;
    private const int PositionMaxLength = 200;
    private const int RankMaxLength = 100;
    private const int ImageUrlMaxLength = 1000;

    private readonly ILoggerService _logger;
    private readonly IUnitOfWork _unitOfWork;

    public LeaderService(ILoggerService logger, IUnitOfWork unitOfWork)
    {
        _logger = logger;
        _unitOfWork = unitOfWork;
    }

    public async Task<List<LeaderDto>> GetAllLeadersAsync()
    {
        _logger.Info("[GetAllLeaders] Requests leader list.");

        var leaders = await _unitOfWork.Leaders.GetQueryable()
            .AsNoTracking()
            .OrderBy(l => l.DisplayOrder)
            .ThenBy(l => l.FullName)
            .ToListAsync();

        _logger.Success($"[GetAllLeaders] Returned {leaders.Count} leaders.");
        return leaders.Select(ToDto).ToList();
    }

    public async Task<LeaderDto> GetLeaderByIdAsync(Guid id)
    {
        _logger.Info($"[GetLeaderById] Requests leader {id}.");

        var leader = await _unitOfWork.Leaders.GetByIdAsync(id);
        if (leader == null)
        {
            _logger.Warn($"[GetLeaderById] Leader {id} not found.");
            throw ErrorHelper.NotFound($"Thủ trưởng với ID {id} không tồn tại.");
        }

        return ToDto(leader);
    }

    public async Task<LeaderDto> CreateLeaderAsync(CreateLeaderDto dto)
    {
        _logger.Info("[CreateLeader] Starts creating leader.");

        if (dto == null)
            throw ErrorHelper.BadRequest("Dữ liệu không hợp lệ.");

        var leader = new Leader
        {
            FullName = RequireText(dto.FullName, "Họ tên thủ trưởng là bắt buộc.", "Họ tên không được vượt quá 256 ký tự.", FullNameMaxLength),
            Position = RequireText(dto.Position, "Chức vụ là bắt buộc.", "Chức vụ không được vượt quá 200 ký tự.", PositionMaxLength),
            Rank = RequireText(dto.Rank, "Cấp bậc là bắt buộc.", "Cấp bậc không được vượt quá 100 ký tự.", RankMaxLength),
            ImageUrl = NormalizeImageUrl(dto.ImageUrl),
            DisplayOrder = dto.DisplayOrder
        };

        await _unitOfWork.Leaders.AddAsync(leader);
        await _unitOfWork.SaveChangesAsync();

        _logger.Success($"[CreateLeader] Leader {leader.Id} created.");
        return ToDto(leader);
    }

    public async Task<LeaderDto> UpdateLeaderAsync(Guid id, UpdateLeaderDto dto)
    {
        _logger.Info($"[UpdateLeader] Starts updating leader {id}.");

        if (dto == null)
            throw ErrorHelper.BadRequest("Dữ liệu không hợp lệ.");

        var leader = await _unitOfWork.Leaders.GetByIdAsync(id);
        if (leader == null)
        {
            _logger.Warn($"[UpdateLeader] Leader {id} not found.");
            throw ErrorHelper.NotFound($"Thủ trưởng với ID {id} không tồn tại.");
        }

        if (dto.FullName != null)
            leader.FullName = RequireText(dto.FullName, "Họ tên thủ trưởng là bắt buộc.", "Họ tên không được vượt quá 256 ký tự.", FullNameMaxLength);

        if (dto.Position != null)
            leader.Position = RequireText(dto.Position, "Chức vụ là bắt buộc.", "Chức vụ không được vượt quá 200 ký tự.", PositionMaxLength);

        if (dto.Rank != null)
            leader.Rank = RequireText(dto.Rank, "Cấp bậc là bắt buộc.", "Cấp bậc không được vượt quá 100 ký tự.", RankMaxLength);

        if (dto.ImageUrl != null)
            leader.ImageUrl = NormalizeImageUrl(dto.ImageUrl);

        if (dto.DisplayOrder.HasValue)
            leader.DisplayOrder = dto.DisplayOrder.Value;

        await _unitOfWork.Leaders.Update(leader);
        await _unitOfWork.SaveChangesAsync();

        _logger.Success($"[UpdateLeader] Leader {id} updated.");
        return ToDto(leader);
    }

    public async Task<bool> DeleteLeaderAsync(Guid id)
    {
        _logger.Info($"[DeleteLeader] Starts deleting leader {id}.");

        var leader = await _unitOfWork.Leaders.GetByIdAsync(id);
        if (leader == null)
        {
            _logger.Warn($"[DeleteLeader] Leader {id} not found.");
            throw ErrorHelper.NotFound($"Thủ trưởng với ID {id} không tồn tại.");
        }

        await _unitOfWork.Leaders.HardRemoveRange(new List<Leader> { leader });
        await _unitOfWork.SaveChangesAsync();

        _logger.Success($"[DeleteLeader] Leader {id} deleted.");
        return true;
    }

    private static string RequireText(string? value, string requiredMessage, string lengthMessage, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw ErrorHelper.BadRequest(requiredMessage);

        var trimmed = value.Trim();
        if (trimmed.Length > maxLength)
            throw ErrorHelper.BadRequest(lengthMessage);

        return trimmed;
    }

    private static string? NormalizeImageUrl(string? imageUrl)
    {
        if (string.IsNullOrWhiteSpace(imageUrl))
            return null;

        var trimmed = imageUrl.Trim();
        if (trimmed.Length > ImageUrlMaxLength)
            throw ErrorHelper.BadRequest("Ảnh không được vượt quá 1000 ký tự.");

        return trimmed;
    }

    private static LeaderDto ToDto(Leader leader)
    {
        return new LeaderDto
        {
            Id = leader.Id,
            FullName = leader.FullName,
            Position = leader.Position,
            Rank = leader.Rank,
            ImageUrl = leader.ImageUrl,
            DisplayOrder = leader.DisplayOrder,
            CreatedAt = leader.CreatedAt,
            UpdatedAt = leader.UpdatedAt
        };
    }
}
