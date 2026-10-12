using congThongTinf.Service.DTOs;

namespace congThongTinf.Service.Interfaces;

public interface ICategoryService
{
    Task<List<CategoryDto>> GetAllCategoriesAsync();

    Task<CategoryDto> GetCategoryByIdAsync(Guid id);

    Task<CategoryDto> CreateCategoryAsync(CreateCategoryDto dto);

    Task<CategoryDto> UpdateCategoryAsync(Guid id, UpdateCategoryDto dto);

    Task<bool> DeleteCategoryAsync(Guid id);

    Task<DashboardCategoryStatsDto> GetDashboardSnapshotAsync();
}
