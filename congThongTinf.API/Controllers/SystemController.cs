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
                    Content = BuildArticleContent(Random.Shared),
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
                    Content = BuildArticleContent(Random.Shared),
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
                    Content = BuildArticleContent(Random.Shared),
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
                    Content = BuildArticleContent(Random.Shared),
                    CategoryId = categories["Tin tức đơn vị"],
                    CoverImageUrl = "https://file3.qdnd.vn/data/images/0/2022/01/15/thuyan/e%20917.jpg?dpi=150&quality=100&w=870",
                    AuthorId = authorId,
                    Status = NewsStatus.Draft,
                    ViewCount = 0,
                    CreatedAt = now.AddHours(-3),
                    UpdatedAt = now.AddHours(-1)
                }
            };

            articles.AddRange(BuildRandomArticles(now, categories.Values.ToList(), await GetSeededAuthorIdsAsync(), 15));

            await _context.News.AddRangeAsync(articles);
            await _context.SaveChangesAsync();
            _logger.Success($"Đã seed {articles.Count} bài viết.");
            return articles.Count;
        }

        private async Task<List<Guid>> GetSeededAuthorIdsAsync()
        {
            var ids = await _context.Users
                .Where(u => u.IsActive)
                .Select(u => u.Id)
                .ToListAsync();

            if (ids.Count == 0)
                ids.Add(await EnsureJournalist(DateTime.UtcNow));

            return ids;
        }

        private static List<News> BuildRandomArticles(DateTime now, List<Guid> categoryIds, List<Guid> authorIds, int count)
        {
            var random = Random.Shared;
            var titles = new[]
            {
                "Đơn vị ra quân huấn luyện đầu tháng",
                "Tổ bay hoàn thành nhiệm vụ bay đêm",
                "Hội thi kỹ thuật bảo dưỡng máy bay",
                "Cán bộ tiêu biểu được biểu dương",
                "Bảo đảm an toàn bay trong mùa mưa",
                "Lớp học nâng cao kỹ năng chỉ huy",
                "Đại hội Chi bộ nhiệm kỳ mới",
                "Diễn tập sẵn sàng chiến đấu cấp đơn vị",
                "Hoạt động thể thao giao hữu giữa các đơn vị",
                "Tăng cường công tác bảo đảm hậu cần",
                "Kiểm tra chất lượng trang bị kỹ thuật",
                "Văn nghệ quần chúng mừng ngày truyền thống",
                "Đoàn viên thanh niên tham gia hiến máu",
                "Tập huấn phòng cháy chữa cháy tại doanh trại",
                "Gặp mặt cựu chiến binh nhân dịp kỷ niệm"
            };
            var summaries = new[]
            {
                "Thông tin cập nhật về hoạt động của đơn vị trong thời gian gần đây.",
                "Tóm tắt kết quả và các nội dung đã triển khai theo kế hoạch.",
                "Nội dung chính được tổng hợp từ báo cáo của các đơn vị trực thuộc."
            };
            var statuses = new[] { NewsStatus.Draft, NewsStatus.Pending, NewsStatus.Published };

            var articles = new List<News>(count);
            for (var i = 0; i < count; i++)
            {
                var status = statuses[random.Next(statuses.Length)];
                var createdAt = now.AddHours(-random.Next(1, 24 * 14));
                articles.Add(new News
                {
                    Id = Guid.NewGuid(),
                    Title = titles[i % titles.Length],
                    Summary = summaries[random.Next(summaries.Length)],
                    Content = BuildArticleContent(random),
                    CategoryId = categoryIds[random.Next(categoryIds.Count)],
                    AuthorId = authorIds[random.Next(authorIds.Count)],
                    Status = status,
                    PublishedAt = status == NewsStatus.Published ? createdAt.AddHours(2) : null,
                    ViewCount = status == NewsStatus.Published ? random.Next(0, 200) : 0,
                    CreatedAt = createdAt,
                    UpdatedAt = createdAt.AddHours(1)
                });
            }

            return articles;
        }

        private static string BuildArticleContent(Random random)
        {
            var paragraphs = new[]
            {
                "Trong bối cảnh nhiệm vụ huấn luyện và sẵn sàng chiến đấu ngày càng được đặt ra cao hơn, các đơn vị thuộc Sư đoàn Không quân 370 tiếp tục đẩy mạnh công tác chuẩn bị, bảo đảm đầy đủ về con người, trang bị và điều kiện thực hiện nhiệm vụ. Nội dung được triển khai đồng bộ từ cấp chi bộ đến từng tổ bay, từng bộ phận kỹ thuật.",
                "Theo báo cáo của các đơn vị, kế hoạch đã được thực hiện đúng tiến độ với nhiều điểm mới. Cán bộ chỉ huy chú trọng rà soát lại quy trình, phân công trách nhiệm rõ ràng cho từng cá nhân và tăng cường kiểm tra thường xuyên trong suốt quá trình triển khai.",
                "Công tác bảo đảm kỹ thuật được coi là khâu then chốt. Các tổ kỹ thuật tiến hành kiểm tra, bảo dưỡng định kỳ theo đúng quy định, đối chiếu sổ sách và ghi chép đầy đủ tình trạng trang bị. Mọi phát hiện bất thường đều được báo cáo kịp thời và xử lý dứt điểm trước khi đưa vào sử dụng.",
                "Song song với huấn luyện chuyên môn, các đơn vị đặc biệt quan tâm đến công tác giáo dục chính trị, tư tưởng cho cán bộ, chiến sĩ. Các buổi sinh hoạt được tổ chức nghiêm túc, bám sát tình hình thực tế, giúp mỗi người nâng cao nhận thức về trách nhiệm và kỷ luật trong quân ngũ.",
                "Về công tác hậu cần, đơn vị đã chủ động rà soát nhu cầu vật tư, nhiên liệu và các điều kiện sinh hoạt phục vụ bộ đội. Việc bảo đảm đầy đủ, kịp thời góp phần giữ vững nhịp độ huấn luyện, đồng thời bảo đảm sức khỏe và đời sống tinh thần cho cán bộ, chiến sĩ trong suốt thời gian làm nhiệm vụ.",
                "Các đại biểu đánh giá cao tinh thần trách nhiệm và ý thức tự giác của lực lượng trực tiếp thực hiện. Nhiều tập thể, cá nhân có thành tích xuất sắc đã được biểu dương, qua đó tạo động lực thi đua lập thành tích chào mừng các dịp kỷ niệm quan trọng của đơn vị trong thời gian tới.",
                "Thời gian tới, đơn vị tiếp tục duy trì các nội dung đã triển khai, đồng thời khắc phục những hạn chế được nêu ra trong quá trình kiểm điểm. Trọng tâm là nâng cao chất lượng huấn luyện thực chất, sát với yêu cầu chiến đấu, bảo đảm an toàn tuyệt đối trong mọi chuyến bay và hoạt động kỹ thuật.",
                "Qua đợt triển khai này, có thể thấy sự phối hợp giữa các cấp, các lực lượng ngày càng chặt chẽ và hiệu quả. Đây là cơ sở quan trọng để Sư đoàn tiếp tục hoàn thành tốt nhiệm vụ được giao, xứng đáng với truyền thống vẻ vang của đơn vị qua các thời kỳ cách mạng.",
                "Tại phần kết luận, lãnh đạo Sư đoàn yêu cầu các đơn vị tiếp tục quán triệt nghiêm túc các chỉ thị, mệnh lệnh cấp trên; chủ động đề xuất giải pháp đổi mới phương pháp huấn luyện; đồng thời thường xuyên tổng kết, rút kinh nghiệm để công tác ngày càng đi vào chiều sâu và đạt hiệu quả cao hơn."
            };

            // Lấy 8 đoạn để bài đạt khoảng 500-600 từ.
            var picked = paragraphs.OrderBy(_ => random.Next()).Take(8);
            return string.Join("\n\n", picked);
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
