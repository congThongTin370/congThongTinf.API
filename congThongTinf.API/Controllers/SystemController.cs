using congThongTinf.Domain;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace congThongTinf.API.Controllers
{
    [ApiController]
    [Route("api/system")]
    public class SystemController : ControllerBase
    {
        private readonly congThongTinfDbContext _context;
        private readonly ILoggerService _logger;



        public SystemController(congThongTinfDbContext context, ILoggerService logger)
        {
            _context = context;
            _logger = logger;
        }

        [HttpPost("seed-all-data")]
        public async Task<IActionResult> SeedData()
        {
            try
            {
                await ClearDatabase(_context);

                // Seed data
                //await SeedRolesAndUsers();
                //await SeedCategories();
                return Ok(ApiResult<object>.Success(new
                {
                    Message = "Data seeded successfully."
                }));
            }
            catch (DbUpdateException dbEx)
            {
                _logger.Error($"Database update error: {dbEx.Message}");
                return StatusCode(500, "Error seeding data: Database issue.");
            }
            catch (Exception ex)
            {
                _logger.Error($"General error: {ex.Message}");
                return StatusCode(500, "Error seeding data: General failure.");
            }
        }

        private async Task ClearDatabase(congThongTinfDbContext context)
        {
            var strategy = context.Database.CreateExecutionStrategy();

            await strategy.ExecuteAsync(async () =>
            {
                await using var transaction = await context.Database.BeginTransactionAsync();

                try
                {
                    _logger.Info("Bắt đầu xóa dữ liệu trong database...");

                    var tablesToDelete = new List<Func<Task>>
                {
                   

                    () => context.Users.ExecuteDeleteAsync(),
                    () => context.Roles.ExecuteDeleteAsync(),
                    () => context.Documents.ExecuteDeleteAsync(),
                    () => context.News.ExecuteDeleteAsync(),
                };

                    foreach (var deleteFunc in tablesToDelete)
                        await deleteFunc();

                    await transaction.CommitAsync();

                    _logger.Success("Xóa sạch dữ liệu trong database thành công.");
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    _logger.Error($"Xóa dữ liệu thất bại: {ex.Message}");
                    throw;
                }
            });
        }

    }
}
