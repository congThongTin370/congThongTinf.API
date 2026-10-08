using congThongTinf.Domain;
using congThongTinf.Domain.Entities;
using congThongTinf.Service.Utils;
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

                var seededUsersRoles = await SeedUsersRoles();
                var seededCount = await SeedNews();
                return Ok(ApiResult<object>.Success(new
                {
                    Message = "Data seeded successfully.",
                    RoleCount = seededUsersRoles.RoleCount,
                    AdminCount = seededUsersRoles.AdminCount,
                    NewsCount = seededCount
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

        [HttpPost("seed-users-roles")]
        public async Task<IActionResult> SeedUsersRolesData()
        {
            try
            {
                var seeded = await SeedUsersRoles();
                return Ok(ApiResult<object>.Success(new
                {
                    Message = "Roles and admin accounts seeded successfully.",
                    RoleCount = seeded.RoleCount,
                    AdminCount = seeded.AdminCount
                }));
            }
            catch (DbUpdateException dbEx)
            {
                _logger.Error($"Database update error: {dbEx.Message}");
                return StatusCode(500, "Error seeding roles: Database issue.");
            }
            catch (Exception ex)
            {
                _logger.Error($"General error: {ex.Message}");
                return StatusCode(500, "Error seeding roles: General failure.");
            }
        }

        [HttpPost("seed-news")]
        public async Task<IActionResult> SeedNewsData()
        {
            try
            {
                var seededCount = await SeedNews();
                return Ok(ApiResult<object>.Success(new
                {
                    Message = "News seeded successfully.",
                    NewsCount = seededCount
                }));
            }
            catch (DbUpdateException dbEx)
            {
                _logger.Error($"Database update error: {dbEx.Message}");
                return StatusCode(500, "Error seeding news: Database issue.");
            }
            catch (Exception ex)
            {
                _logger.Error($"General error: {ex.Message}");
                return StatusCode(500, "Error seeding news: General failure.");
            }
        }

        [HttpPost("clear-database")]
        public async Task<IActionResult> ClearDatabaseData()
        {
            try
            {
                await ClearDatabase(_context);
                return Ok(ApiResult<object>.Success(new
                {
                    Message = "Database cleared successfully."
                }));
            }
            catch (DbUpdateException dbEx)
            {
                _logger.Error($"Database update error: {dbEx.Message}");
                return StatusCode(500, "Error clearing database: Database issue.");
            }
            catch (Exception ex)
            {
                _logger.Error($"General error: {ex.Message}");
                return StatusCode(500, "Error clearing database: General failure.");
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
                        () => context.News.ExecuteDeleteAsync(),
                        () => context.Documents.ExecuteDeleteAsync(),
                        () => context.Users.ExecuteDeleteAsync(),
                        () => context.Roles.ExecuteDeleteAsync(),
                        () => context.Categories.ExecuteDeleteAsync(),
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

        private async Task<(int RoleCount, int AdminCount)> SeedUsersRoles()
        {
            var now = DateTime.UtcNow;
            var roles = new (RoleType Name, string Description)[]
            {
                (RoleType.Admin, "Xem thống kê, quản lý tài khoản và vai trò, quản lý văn bản nội bộ."),
                (RoleType.Journalist, "Tạo, sửa, xuất bản và quản lý bài viết của mình.")
            };

            var names = roles.Select(r => r.Name).ToArray();
            var existing = await _context.Roles
                .Where(r => names.Contains(r.Name))
                .Select(r => r.Name)
                .ToListAsync();

            var addedRoles = 0;
            foreach (var item in roles)
            {
                if (existing.Contains(item.Name))
                    continue;

                _context.Roles.Add(new Role
                {
                    Id = Guid.NewGuid(),
                    Name = item.Name,
                    Description = item.Description,
                    CreatedAt = now,
                    UpdatedAt = now
                });
                addedRoles++;
            }

            if (addedRoles > 0)
                await _context.SaveChangesAsync();

            var adminRole = await _context.Roles.FirstAsync(r => r.Name == RoleType.Admin);
            var admins = new (string Username, string Email, string FullName)[]
            {
                ("admin", "admin@370.local", "Quản trị viên 1"),
                ("admin2", "admin2@370.local", "Quản trị viên 2")
            };

            var emails = admins.Select(a => a.Email).ToArray();
            var existingEmails = await _context.Users
                .Where(u => emails.Contains(u.Email))
                .Select(u => u.Email)
                .ToListAsync();

            var hasher = new PasswordHasher();
            var addedAdmins = 0;
            foreach (var item in admins)
            {
                if (existingEmails.Contains(item.Email))
                    continue;

                _context.Users.Add(new User
                {
                    Id = Guid.NewGuid(),
                    Username = item.Username,
                    Email = item.Email,
                    PasswordHash = hasher.HashPassword("Admin@123") ?? string.Empty,
                    FullName = item.FullName,
                    RoleId = adminRole.Id,
                    IsActive = true,
                    CreatedAt = now,
                    UpdatedAt = now
                });
                addedAdmins++;
            }

            if (addedAdmins > 0)
                await _context.SaveChangesAsync();

            _logger.Success($"Đã seed {addedRoles} vai trò và {addedAdmins} tài khoản admin.");
            return (addedRoles, addedAdmins);
        }

        private async Task<int> SeedNews()
        {
            var now = DateTime.UtcNow;
            var categories = await EnsureCategories(now);
            var authorId = await EnsureJournalist(now);

            var articles = new List<News>
            {
                new()
                {
                    Id = Guid.NewGuid(),
                    Title = "Sư đoàn 370 tổ chức hội nghị giao ban tháng 10",
                    Summary = "Các đơn vị báo cáo kết quả huấn luyện, sẵn sàng chiến đấu và công tác bảo đảm trong tháng.",
                    Content = "Sáng ngày 8/10, Sư đoàn Không quân 370 tổ chức hội nghị giao ban tháng. Chỉ huy các đơn vị đã báo cáo tiến độ huấn luyện bay, công tác kỹ thuật và bảo đảm hậu cần. Hội nghị thống nhất kế hoạch duy trì sẵn sàng chiến đấu trong quý IV.",
                    CategoryId = categories["Tin tức đơn vị"],
                    AuthorId = authorId,
                    Status = NewsStatus.Published,
                    PublishedAt = now.AddDays(-2),
                    ViewCount = 48,
                    CreatedAt = now.AddDays(-3),
                    UpdatedAt = now.AddDays(-2)
                },
                new()
                {
                    Id = Guid.NewGuid(),
                    Title = "Trung đoàn hoàn thành đợt bay huấn luyện đêm",
                    Summary = "Phi công và tổ bay hoàn thành đủ giáo án bay đêm trong điều kiện thời tiết phức tạp.",
                    Content = "Trong ba ngày vừa qua, Trung đoàn đã tổ chức bay huấn luyện đêm theo đúng kế hoạch. Các tổ bay bảo đảm an toàn tuyệt đối, hoàn thành nội dung cất cánh, tiếp cận và hạ cánh trong điều kiện tầm nhìn hạn chế.",
                    CategoryId = categories["Huấn luyện"],
                    AuthorId = authorId,
                    Status = NewsStatus.Published,
                    PublishedAt = now.AddDays(-1),
                    ViewCount = 31,
                    CreatedAt = now.AddDays(-2),
                    UpdatedAt = now.AddDays(-1)
                },
                new()
                {
                    Id = Guid.NewGuid(),
                    Title = "Sinh hoạt chính trị chủ đề truyền thống Sư đoàn",
                    Summary = "Cán bộ, chiến sĩ ôn lại truyền thống chiến đấu và những tấm gương điển hình của đơn vị.",
                    Content = "Buổi sinh hoạt chính trị được tổ chức tại hội trường sư đoàn, với nội dung ôn lại chặng đường xây dựng, chiến đấu và trưởng thành. Các đại biểu đã trao đổi về việc giữ vững bản lĩnh chính trị trong tình hình mới.",
                    CategoryId = categories["Chính trị"],
                    AuthorId = authorId,
                    Status = NewsStatus.Pending,
                    ViewCount = 0,
                    CreatedAt = now.AddHours(-8),
                    UpdatedAt = now.AddHours(-6)
                },
                new()
                {
                    Id = Guid.NewGuid(),
                    Title = "Kế hoạch kiểm tra công tác kỹ thuật hàng không",
                    Summary = "Dự thảo kế hoạch kiểm tra bảo dưỡng máy bay và trang bị mặt đất trong tháng tới.",
                    Content = "Ban Kỹ thuật đang hoàn thiện kế hoạch kiểm tra định kỳ trang bị kỹ thuật hàng không. Nội dung dự kiến gồm bảo dưỡng định kỳ, kiểm tra sổ sách và rà soát vật tư dự trữ.",
                    CategoryId = categories["Tin tức đơn vị"],
                    AuthorId = authorId,
                    Status = NewsStatus.Draft,
                    ViewCount = 0,
                    CreatedAt = now.AddHours(-3),
                    UpdatedAt = now.AddHours(-1)
                }
            };

            await _context.News.AddRangeAsync(articles);
            await _context.SaveChangesAsync();
            _logger.Success($"Đã seed {articles.Count} bài viết.");
            return articles.Count;
        }

        private async Task<Dictionary<string, Guid>> EnsureCategories(DateTime now)
        {
            var names = new (string Name, string Description, int Order)[]
            {
                ("Tin tức đơn vị", "Tin hoạt động của Sư đoàn Không quân 370", 1),
                ("Huấn luyện", "Tin huấn luyện bay và sẵn sàng chiến đấu", 2),
                ("Chính trị", "Công tác đảng, công tác chính trị", 3)
            };

            var categoryNames = names.Select(n => n.Name).ToArray();
            var existing = await _context.Categories
                .Where(c => categoryNames.Contains(c.Name))
                .ToListAsync();

            foreach (var item in names)
            {
                if (existing.Any(c => c.Name == item.Name))
                    continue;

                var category = new Category
                {
                    Id = Guid.NewGuid(),
                    Name = item.Name,
                    Description = item.Description,
                    DisplayOrder = item.Order,
                    IsActive = true,
                    CreatedAt = now,
                    UpdatedAt = now
                };
                _context.Categories.Add(category);
                existing.Add(category);
            }

            await _context.SaveChangesAsync();
            return existing.ToDictionary(c => c.Name, c => c.Id);
        }

        private async Task<Guid> EnsureJournalist(DateTime now)
        {
            var author = await _context.Users.FirstOrDefaultAsync();
            if (author != null)
                return author.Id;

            var role = await _context.Roles.FirstOrDefaultAsync(r => r.Name == RoleType.Journalist);
            if (role == null)
            {
                role = new Role
                {
                    Id = Guid.NewGuid(),
                    Name = RoleType.Journalist,
                    Description = "Biên tập viên tin tức nội bộ",
                    CreatedAt = now,
                    UpdatedAt = now
                };
                _context.Roles.Add(role);
            }

            author = new User
            {
                Id = Guid.NewGuid(),
                Username = "journalist",
                Email = "journalist@370.local",
                PasswordHash = new PasswordHasher().HashPassword("Journalist@123") ?? string.Empty,
                FullName = "Phóng viên Sư đoàn",
                RoleId = role.Id,
                IsActive = true,
                CreatedAt = now,
                UpdatedAt = now
            };
            _context.Users.Add(author);
            await _context.SaveChangesAsync();
            return author.Id;
        }

    }
}
