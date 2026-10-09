using congThongTinf.Service.DTOs;
using congThongTinf.Service.Interfaces;
using congThongTinf.Service.Utils;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace congThongTinf.API.Controllers;

[Route("api/categories")]
[ApiController]
public class CategoryController : ControllerBase
{
    private readonly ICategoryService _categoryService;

    public CategoryController(ICategoryService categoryService)
    {
        _categoryService = categoryService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(ApiResult<List<CategoryDto>>), 200)]
    public async Task<IActionResult> GetAll()
    {
        try
        {
            var result = await _categoryService.GetAllCategoriesAsync();
            return Ok(ApiResult<List<CategoryDto>>.Success(result, "200", "Lấy danh sách danh mục thành công."));
        }
        catch (Exception ex)
        {
            var statusCode = ExceptionUtils.ExtractStatusCode(ex);
            var errorResponse = ExceptionUtils.CreateErrorResponse<List<CategoryDto>>(ex);
            return StatusCode(statusCode, errorResponse);
        }
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ApiResult<CategoryDto>), 200)]
    [ProducesResponseType(typeof(ApiResult<CategoryDto>), 404)]
    public async Task<IActionResult> GetById(Guid id)
    {
        try
        {
            var result = await _categoryService.GetCategoryByIdAsync(id);
            return Ok(ApiResult<CategoryDto>.Success(result, "200", "Lấy danh mục thành công."));
        }
        catch (Exception ex)
        {
            var statusCode = ExceptionUtils.ExtractStatusCode(ex);
            var errorResponse = ExceptionUtils.CreateErrorResponse<CategoryDto>(ex);
            return StatusCode(statusCode, errorResponse);
        }
    }

    [Authorize(Roles = "Admin")]
    [HttpPost]
    [ProducesResponseType(typeof(ApiResult<CategoryDto>), 200)]
    [ProducesResponseType(typeof(ApiResult<CategoryDto>), 409)]
    public async Task<IActionResult> Create([FromBody] CreateCategoryDto dto)
    {
        try
        {
            var result = await _categoryService.CreateCategoryAsync(dto);
            return Ok(ApiResult<CategoryDto>.Success(result, "200", "Tạo danh mục thành công."));
        }
        catch (Exception ex)
        {
            var statusCode = ExceptionUtils.ExtractStatusCode(ex);
            var errorResponse = ExceptionUtils.CreateErrorResponse<CategoryDto>(ex);
            return StatusCode(statusCode, errorResponse);
        }
    }

    [Authorize(Roles = "Admin")]
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(ApiResult<CategoryDto>), 200)]
    [ProducesResponseType(typeof(ApiResult<CategoryDto>), 404)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateCategoryDto dto)
    {
        try
        {
            var result = await _categoryService.UpdateCategoryAsync(id, dto);
            return Ok(ApiResult<CategoryDto>.Success(result, "200", "Cập nhật danh mục thành công."));
        }
        catch (Exception ex)
        {
            var statusCode = ExceptionUtils.ExtractStatusCode(ex);
            var errorResponse = ExceptionUtils.CreateErrorResponse<CategoryDto>(ex);
            return StatusCode(statusCode, errorResponse);
        }
    }

    [Authorize(Roles = "Admin")]
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(typeof(ApiResult<bool>), 200)]
    [ProducesResponseType(typeof(ApiResult<bool>), 404)]
    public async Task<IActionResult> Delete(Guid id)
    {
        try
        {
            var result = await _categoryService.DeleteCategoryAsync(id);
            return Ok(ApiResult<bool>.Success(result, "200", "Xóa danh mục thành công."));
        }
        catch (Exception ex)
        {
            var statusCode = ExceptionUtils.ExtractStatusCode(ex);
            var errorResponse = ExceptionUtils.CreateErrorResponse<bool>(ex);
            return StatusCode(statusCode, errorResponse);
        }
    }
}
