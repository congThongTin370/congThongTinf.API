using congThongTinf.Domain.Entities;
using congThongTinf.Service.DTOs;
using congThongTinf.Service.Interfaces;
using congThongTinf.Service.Interfaces.Commons;
using congThongTinf.Service.Utils;
using Microsoft.EntityFrameworkCore;

namespace congThongTinf.Service.Services;

public class CategoryService : ICategoryService
{
    private readonly ILoggerService _logger;
    private readonly IUnitOfWork _unitOfWork;

    public CategoryService(ILoggerService logger, IUnitOfWork unitOfWork)
    {
        _logger = logger;
        _unitOfWork = unitOfWork;
    }

    public async Task<List<CategoryDto>> GetAllCategoriesAsync()
    {
        _logger.Info("[GetAllCategories] Requests category list.");

        var categories = await _unitOfWork.Categories.GetQueryable()
            .AsNoTracking()
            .Where(c => c.IsActive)
            .OrderBy(c => c.DisplayOrder)
            .ThenBy(c => c.Name)
            .ToListAsync();

        _logger.Success($"[GetAllCategories] Returned {categories.Count} categories.");
        return categories.Select(ToDto).ToList();
    }

    public async Task<DashboardCategoryStatsDto> GetDashboardSnapshotAsync()
    {
        var categories = _unitOfWork.Categories.GetQueryable().AsNoTracking();
        return new DashboardCategoryStatsDto
        {
            Total = await categories.CountAsync(),
            Active = await categories.CountAsync(c => c.IsActive)
        };
    }

    public async Task<CategoryDto> GetCategoryByIdAsync(Guid id)
    {
        _logger.Info($"[GetCategoryById] Requests category {id}.");

        var category = await _unitOfWork.Categories.FirstOrDefaultAsync(c => c.Id == id && c.IsActive);
        if (category == null)
        {
            _logger.Warn($"[GetCategoryById] Category {id} not found.");
            throw ErrorHelper.NotFound(ErrorMessages.CategoryNotFound);
        }

        return ToDto(category);
    }

    public async Task<CategoryDto> CreateCategoryAsync(CreateCategoryDto dto)
    {
        _logger.Info("[CreateCategory] Starts creating category.");

        if (string.IsNullOrWhiteSpace(dto.Name))
            throw ErrorHelper.BadRequest(ErrorMessages.CategoryNameRequired);

        var name = dto.Name.Trim();
        if (await NameExistsAsync(name))
        {
            _logger.Warn($"[CreateCategory] Name '{name}' already exists.");
            throw ErrorHelper.Conflict(ErrorMessages.CategoryNameAlreadyExists);
        }

        var category = new Category
        {
            Name = name,
            Description = dto.Description?.Trim() ?? string.Empty,
            Icon = dto.Icon,
            DisplayOrder = dto.DisplayOrder,
            IsActive = dto.IsActive,
        };

        await _unitOfWork.Categories.AddAsync(category);
        await _unitOfWork.SaveChangesAsync();

        _logger.Success($"[CreateCategory] Category {category.Id} created.");
        return ToDto(category);
    }

    public async Task<CategoryDto> UpdateCategoryAsync(Guid id, UpdateCategoryDto dto)
    {
        _logger.Info($"[UpdateCategory] Starts updating category {id}.");

        var category = await _unitOfWork.Categories.FirstOrDefaultAsync(c => c.Id == id && c.IsActive);
        if (category == null)
        {
            _logger.Warn($"[UpdateCategory] Category {id} not found.");
            throw ErrorHelper.NotFound(ErrorMessages.CategoryNotFound);
        }

        if (dto.Name != null)
        {
            if (string.IsNullOrWhiteSpace(dto.Name))
                throw ErrorHelper.BadRequest(ErrorMessages.CategoryNameRequired);

            var name = dto.Name.Trim();
            if (!string.Equals(name, category.Name, StringComparison.OrdinalIgnoreCase)
                && await NameExistsAsync(name))
                throw ErrorHelper.Conflict(ErrorMessages.CategoryNameAlreadyExists);

            category.Name = name;
        }

        if (dto.Description != null)
            category.Description = dto.Description.Trim();

        if (dto.Icon != null)
            category.Icon = dto.Icon;

        if (dto.DisplayOrder.HasValue)
            category.DisplayOrder = dto.DisplayOrder.Value;

        if (dto.IsActive.HasValue)
            category.IsActive = dto.IsActive.Value;

        await _unitOfWork.Categories.Update(category);
        await _unitOfWork.SaveChangesAsync();

        _logger.Success($"[UpdateCategory] Category {id} updated.");
        return ToDto(category);
    }

    public async Task<bool> DeleteCategoryAsync(Guid id)
    {
        _logger.Info($"[DeleteCategory] Starts deleting category {id}.");

        var category = await _unitOfWork.Categories.FirstOrDefaultAsync(c => c.Id == id && c.IsActive);
        if (category == null)
        {
            _logger.Warn($"[DeleteCategory] Category {id} not found.");
            throw ErrorHelper.NotFound(ErrorMessages.CategoryNotFound);
        }

        var hasArticles = await _unitOfWork.News.GetQueryable()
            .AnyAsync(n => n.CategoryId == id);
        if (hasArticles)
        {
            _logger.Warn($"[DeleteCategory] Category {id} still has articles.");
            throw ErrorHelper.Conflict(ErrorMessages.CategoryDeleteHasArticles);
        }

        category.IsActive = false;

        await _unitOfWork.Categories.Update(category);
        await _unitOfWork.SaveChangesAsync();

        _logger.Success($"[DeleteCategory] Category {id} soft deleted.");
        return true;
    }

    private async Task<bool> NameExistsAsync(string name)
    {
        return await _unitOfWork.Categories.GetQueryable()
            .AnyAsync(c => c.IsActive && c.Name.ToLower() == name.ToLower());
    }

    private static CategoryDto ToDto(Category category)
    {
        return new CategoryDto
        {
            Id = category.Id,
            Name = category.Name,
            Description = category.Description,
            Icon = category.Icon,
            DisplayOrder = category.DisplayOrder,
            IsActive = category.IsActive,
            CreatedAt = category.CreatedAt,
            UpdatedAt = category.UpdatedAt,
        };
    }
}
