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
                var leaderCount = await SeedLeaders();
                return Ok(ApiResult<object>.Success(new
                {
                    Message = "Data seeded successfully.",
                    RoleCount = seededUsersRoles.RoleCount,
                    AdminCount = seededUsersRoles.AdminCount,
                    JournalistCount = seededUsersRoles.JournalistCount,
                    NewsCount = seededCount,
                    LeaderCount = leaderCount
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
                    Message = "Roles, admin and journalist accounts seeded successfully.",
                    RoleCount = seeded.RoleCount,
                    AdminCount = seeded.AdminCount,
                    JournalistCount = seeded.JournalistCount
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

        [HttpPost("seed-leaders")]
        public async Task<IActionResult> SeedLeadersData()
        {
            try
            {
                var leaderCount = await SeedLeaders();
                return Ok(ApiResult<object>.Success(new
                {
                    Message = "Leaders seeded successfully.",
                    LeaderCount = leaderCount
                }));
            }
            catch (DbUpdateException dbEx)
            {
                _logger.Error($"Database update error: {dbEx.Message}");
                return StatusCode(500, "Error seeding leaders: Database issue.");
            }
            catch (Exception ex)
            {
                _logger.Error($"General error: {ex.Message}");
                return StatusCode(500, "Error seeding leaders: General failure.");
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
                        () => context.Leaders.ExecuteDeleteAsync(),
                        () => context.Banners.ExecuteDeleteAsync(),
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

        private async Task<(int RoleCount, int AdminCount, int JournalistCount)> SeedUsersRoles()
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
            var journalistRole = await _context.Roles.FirstAsync(r => r.Name == RoleType.Journalist);
            var hasher = new PasswordHasher();

            var addedAdmins = await SeedUsers(
                now,
                adminRole.Id,
                hasher,
                "Admin@123",
                new (string Username, string Email, string FullName)[]
                {
                    ("admin", "admin@370.local", "Quản trị viên 1"),
                    ("admin2", "admin2@370.local", "Quản trị viên 2")
                });

            var addedJournalists = await SeedUsers(
                now,
                journalistRole.Id,
                hasher,
                "123321@",
                new (string Username, string Email, string FullName)[]
                {
                    ("nhabao1", "nhabao1@gmail.com", "Trần Vũ Phương Nam"),
                    ("nhabao2", "nhabao2@gmail.com", "Nhà báo 2")
                });

            _logger.Success($"Đã seed {addedRoles} vai trò, {addedAdmins} tài khoản admin và {addedJournalists} tài khoản phóng viên.");
            return (addedRoles, addedAdmins, addedJournalists);
        }

        private async Task<int> SeedUsers(
            DateTime now,
            Guid roleId,
            PasswordHasher hasher,
            string password,
            (string Username, string Email, string FullName)[] users)
        {
            var emails = users.Select(u => u.Email).ToArray();
            var existingEmails = await _context.Users
                .Where(u => emails.Contains(u.Email))
                .Select(u => u.Email)
                .ToListAsync();

            var added = 0;
            foreach (var item in users)
            {
                if (existingEmails.Contains(item.Email))
                    continue;

                _context.Users.Add(new User
                {
                    Id = Guid.NewGuid(),
                    Username = item.Username,
                    Email = item.Email,
                    PasswordHash = hasher.HashPassword(password) ?? string.Empty,
                    FullName = item.FullName,
                    RoleId = roleId,
                    IsActive = true,
                    CreatedAt = now,
                    UpdatedAt = now
                });
                added++;
            }

            if (added > 0)
                await _context.SaveChangesAsync();

            return added;
        }

        private async Task<int> SeedLeaders()
        {
            var now = DateTime.UtcNow;
            var leaders = new (string Position, string Rank, string FullName, int DisplayOrder)[]
            {
                ("Sư đoàn trưởng", "Đại tá", "Cù Đức Hường", 1),
                ("Chính ủy", "Đại tá", "Lê Văn Hợi", 2),
                ("Phó sư đoàn trưởng / TMT", "Đại tá", "Đỗ Mạnh Hùng", 3),
                ("Phó sư đoàn trưởng / Quân sự", "Đại tá", "Phan Việt Anh", 4),
                ("Phó sư đoàn trưởng / Quân huấn", "Đại tá", "Trần Đức Ba", 5),
                ("Phó Chính ủy / kiêm CNUBKT", "Đại tá", "Nguyễn Quốc Long", 6)
            };

            var names = leaders.Select(l => l.FullName).ToArray();
            var existing = await _context.Leaders
                .Where(l => names.Contains(l.FullName))
                .Select(l => l.FullName)
                .ToListAsync();

            var added = 0;
            foreach (var leader in leaders)
            {
                if (existing.Contains(leader.FullName))
                    continue;

                _context.Leaders.Add(new Leader
                {
                    Id = Guid.NewGuid(),
                    FullName = leader.FullName,
                    Position = leader.Position,
                    Rank = leader.Rank,
                    DisplayOrder = leader.DisplayOrder,
                    CreatedAt = now,
                    UpdatedAt = now
                });
                added++;
            }

            if (added > 0)
                await _context.SaveChangesAsync();

            _logger.Success($"Đã seed {added} lãnh đạo đơn vị.");
            return added;
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
                    Title = "Đảng ủy Sư đoàn 370 tổ chức Hội thi Bí thư chi bộ năm 2026",
                    Summary = "Trong 2 ngày (19 và 20-8), Đảng ủy Sư đoàn 370 tổ chức Hội thi Bí thư chi bộ năm 2026. Theo dõi, chỉ đạo Hội thi có Đại tá Lê Văn Hợi - Bí thư Đảng ủy, Chính ủy Sư đoàn; các đồng chí trong Thường vụ Đảng ủy, chỉ huy Sư đoàn; đại biểu Phòng Tổ chức, Cục Chính trị, Quân chủng. Tham gia Hội thi có các đồng chí bí thư chi bộ đạt thành tích cao tại hội thi bí thư chi bộ cấp cơ sở trong toàn Đảng bộ Sư đoàn.",
                    Content = "![image](https://pub-f9c31e10d1244c9cb46f01196c447ad2.r2.dev/news/d11d9e26-a98f-42b3-8b79-655776c242ad/bf47c61894894cb685cd6aa8b0f2255f.jpg)Các đại biểu dự khai mạc hội thi chụp ảnh lưu niệm với các thí sinh dự thi\r\n\r\nTại Hội thi, các thí sinh tham gia các nội dung: Thi chấm điểm sổ sách; thi lý thuyết bằng hình thức trắc nghiệm, vấn đáp; thi thực hành soạn thảo văn kiện và chủ trì hội nghị chi bộ. Ở phần thi chấm điểm sổ sách, Ban Giám khảo tập trung đánh giá hệ thống sổ sách công tác Đảng, công tác chính trị của chi bộ; các nghị quyết, kế hoạch, chương trình công tác; hồ sơ kiểm tra, giám sát... Phần thi lý thuyết gồm 30 câu hỏi trắc nghiệm và nội dung vấn đáp về kiến thức chuyên môn. Đối với phần thi thực hành, các thí sinh thực hành soạn thảo văn kiện trên máy tính theo nội dung câu hỏi của Ban Giám khảo; thực hành chủ trì hội nghị chi bộ và xử lý các tình huống gắn với thực tiễn công tác xây dựng Đảng, hoạt động lãnh đạo của chi bộ.\r\n\r\nVới sự chuẩn bị chu đáo, các thí sinh đã thể hiện khá toàn diện kiến thức, kỹ năng, phương pháp điều hành và khả năng vận dụng các nguyên tắc, quy định của Đảng vào thực tiễn. Nhiều đồng chí có sự chuẩn bị công phu, trả lời đúng trọng tâm, xử lý tình huống linh hoạt, sát thực tế đơn vị.\r\n\r\n![image](https://pub-f9c31e10d1244c9cb46f01196c447ad2.r2.dev/news/d11d9e26-a98f-42b3-8b79-655776c242ad/647db32e16e7479a87637307f7e0dd79.jpg)Các thi sinh thực hiện phần thi lý thuyết.Phát biểu bế mạc Hội thi, Đại tá Lê Văn Hợi - Bí thư Đảng ủy, Chính ủy Sư đoàn ghi nhận, biểu dương tinh thần trách nhiệm, sự nỗ lực, cố gắng của các thí sinh và các cơ quan, đơn vị trong công tác chuẩn bị, tổ chức Hội thi. Đồng chí nhấn mạnh, kết quả Hội thi không chỉ là cơ sở để đánh giá năng lực của đội ngũ bí thư chi bộ mà quan trọng hơn, còn góp phần nâng cao nhận thức, trách nhiệm và phương pháp tiến hành công tác xây dựng Đảng ở cơ sở. Đồng thời yêu cầu các cấp ủy, tổ chức đảng tiếp tục quan tâm bồi dưỡng, rèn luyện đội ngũ bí thư chi bộ; chú trọng đổi mới nội dung, phương pháp sinh hoạt; nâng cao chất lượng ra nghị quyết và tổ chức thực hiện nghị quyết; gắn công tác xây dựng Đảng với lãnh đạo thực hiện thắng lợi nhiệm vụ chính trị của từng cơ quan, đơn vị.![image](https://pub-f9c31e10d1244c9cb46f01196c447ad2.r2.dev/news/d11d9e26-a98f-42b3-8b79-655776c242ad/6c3214693efe480394cd284aa5314d53.jpg)Đại tá Lê Văn Hợi - Bí thư Đảng ủy, Chính ủy Sư đoàn 370 trao giấy khen tặng các thí sinh đạt giải Nhất và giải Nhì tại Hội thi.Kết thúc Hội thi, Ban Tổ chức đã công nhận 15 đồng chí đạt giỏi; trao 2 giải Nhất, 3 giải Nhì, 4 giải Ba và 6 giải khuyến khích cho các thí sinh đạt kết quả cao tại Hội thi.",
                    CategoryId = categories["Tin tức đơn vị"],
                    CoverImageUrl = "https://file3.qdnd.vn/data/images/0/2022/01/15/thuyan/e%20917.jpg?dpi=150&quality=100&w=870",
                    AuthorId = authorId,
                    Status = NewsStatus.Draft,
                    ViewCount = 0,
                    CreatedAt = now.AddHours(-3),
                    UpdatedAt = now.AddHours(-1)
                },
                 new()
                {
                    Id = Guid.NewGuid(),
                    Title = "Quân chủng Phòng không - Không quân kiểm tra công tác tiếp nhận và chuẩn bị huấn luyện chiến sĩ mới tại Sư đoàn 367 và Sư đoàn 370",
                    Summary="Trong 2 ngày (10 và 11-3), Đoàn kiểm tra của Quân chủng PK-KQ do Thiếu tướng Phạm Văn Tính - Phó Tư lệnh Quân chủng làm Trưởng đoàn đã tiến hành kiểm tra công tác tiếp nhận và chuẩn bị huấn luyện chiến sĩ mới tại Sư đoàn 367 và Sư đoàn 370. Tham gia đoàn kiểm tra có Đại tá Nguyễn Tuấn Hiệp - Phó Chủ nhiệm Hậu cần - Kỹ thuật Quân chủng PK-KQ; đại diện một số phòng, ban chức năng của Quân chủng PK-KQ.",
                    Content = "Thiếu tướng Phạm Văn Tính -  Phó Tư lệnh Quân chủng PK-KQ trao đổi, động viên chiến sĩ mới Tiểu đoàn Huấn luyện chiến sĩ mới, Sư đoàn 370.\r\n\r\nTại các Sư đoàn, Đoàn kiểm tra đã nghe chỉ huy đơn vị báo cáo kết quả công tác tiếp nhận và chuẩn bị huấn luyện chiến sĩ mới của đơn vị; trực tiếp kiểm tra công tác tuyển chọn, tiếp nhận, tổ chức biên chế quân số của các đơn vị; công tác chuẩn bị mọi mặt cho huấn luyện chiến sĩ mới, bao gồm: Công tác bảo đảm vật chất huấn luyện, xây dựng chính quy; công tác bảo đảm thao trường bãi tập, trường bắn; tổ chức biên chế và tập huấn khung huấn luyện chiến sĩ mới; hoạt động công tác Đảng, công tác chính trị; công tác tác bảo đảm hậu cần, kỹ thuật.\r\n\r\nTrong thời gian qua, Sư đoàn 367 và Sư đoàn 370 đã tích cực, chủ động làm tốt công tác chuẩn bị mọi mặt cho tiếp nhận và huấn luyện chiến sĩ mới; tổ chức tiếp nhận chiến sĩ mới tại các tỉnh trên địa bàn Quân khu 7 và Quân khu 9 bảo đảm chặt chẽ an toàn. Sau tiếp nhận, các đơn vị đã tiến hành cấp phát bổ sung quân trang, doanh cụ, sắp xếp nơi ăn ở, sinh hoạt cho chiến sĩ mới; tổ chức khám phúc tra sức khỏe; tổ chức duy trì nghiêm nền nếp chính quy, quản lý chặt chẽ quân số, tăng cường nắm,quản lý tư tưởng bộ đội.\r\n\r\nCác đơn vị đã chủ động rà soát, kiện toàn đội ngũ cán bộ khung huấn luyện; tổ chức tập huấn, bồi dưỡng thống nhất nội dung, phương pháp huấn luyện theo chương trình quy định; tích cực củng cố doanh trại, thao trường, bãi tập; soạn giáo án, bổ sung hệ thống mô hình, học cụ, vật chất huấn luyện; duy trì có chất lượng, hiệu quả hoạt động công tác Đảng, công tác chính trị; bảo đảm các tiêu chuẩn đời sống tinh thần; bảo đảm tốt công tác hậu cần, kỹ thuật cho huấn luyện chiến sĩ mới.\r\n\r\n![image](https://pub-f9c31e10d1244c9cb46f01196c447ad2.r2.dev/news/d11d9e26-a98f-42b3-8b79-655776c242ad/0ba46cd08d334e0b856276c6ecd58385.png)Kết luận kiểm tra, Thiếu tướng Phạm Văn Tính biểu dương tinh thần tích cực, chủ động của các đơn vị trong tiếp nhận và chuẩn bị huấn luyện chiến sĩ mới năm 2026; đồng thời yêu cầu các đơn vị tiếp tục tổ chức quán triệt và triển khai làm tốt công tác chuẩn bị cho nhiệm vụ huấn luyện chiến sĩ mới năm 2026; thường xuyên nắm chắc tình hình tư tưởng chiến sĩ mới, không để bị động, bất ngờ về tư tưởng; tiếp tục hoàn thiện hệ thống giáo án theo hướng dẫn, thục luyện thuần thục động tác, nắm chắc nội dung, phương pháp tổ chức hướng dẫn luyện tập sát với đối tượng; nghiên cứu cải tiến mô hình, học cụ, trang thiết bị phục vụ cho nhiệm vụ huấn luyện; tiếp tục củng cố hệ thống doanh trại, nhà ăn, nhà bếp, duy trì bảo đảm  tốt hậu cần, kỹ thuật cho nhiệm vụ huấn luyện chiến sĩ mới; chú trọng bảo đảm quân y, chống say nắng, say nóng cho bộ đội trong quá trình tổ chức huấn luyện.",
                    CategoryId = categories["Tin tức đơn vị"],
                    CoverImageUrl = "https://pub-f9c31e10d1244c9cb46f01196c447ad2.r2.dev/news/d11d9e26-a98f-42b3-8b79-655776c242ad/b899f4b525194bc2ab4d8cd292647559.jpg",
                    AuthorId = authorId,
                    Status = NewsStatus.Draft,
                    ViewCount = 0,
                    CreatedAt = now.AddHours(-3),
                    UpdatedAt = now.AddHours(-1)
                },
                 new()
                {
                    Id = Guid.NewGuid(),
                    Title = "Sư đoàn 370 tổ chức các hoạt động tri ân nhân dịp kỷ niệm 79 năm Ngày Thương binh - Liệt sĩ (27-7-1947 / 27-7-2026)",
                    Summary = "Nhân kỷ niệm 79 năm Ngày Thương binh - Liệt sĩ (27-7-1947 / 27-7-2026), Sư đoàn 370 đã tổ chức nhiều hoạt động thiết thực, ý nghĩa nhằm tri ân các Anh hùng liệt sĩ, thương binh, bệnh binh, gia đình chính sách và người có công với cách mạng.",
                    Content = "> Đoàn công tác của Sư đoàn 370 dâng hương tưởng niệm các Anh hùng liệt sĩ tại Công viên Lê Thị Riêng, TP Hồ Chí Minh.\r\n\r\n![image](https://pub-f9c31e10d1244c9cb46f01196c447ad2.r2.dev/news/d11d9e26-a98f-42b3-8b79-655776c242ad/8e8c20031fe64a7abcaa7ca099167ad0.png)Lãnh đạo, chỉ huy Sư đoàn 370 gặp mặt, tặng quà các đồng chí thương binh, thân nhân gia đình liệt sĩ đang công tác tại Sư đoàn.Các cơ quan, đơn vị trong Sư đoàn đã đẩy mạnh tuyên truyền chủ trương, đường lối của Đảng, chính sách, pháp luật của Nhà nước đối với người có công; giáo dục truyền thống cách mạng, nâng cao nhận thức, trách nhiệm của cán bộ, chiến sĩ trong thực hiện phong trào “Đền ơn đáp nghĩa”. Đồng thời, phối hợp chặt chẽ với cấp ủy, chính quyền địa phương tổ chức các hoạt động thăm hỏi, tặng quà thương binh, gia đình liệt sĩ, gia đình chính sách; tham gia tu sửa, vệ sinh nghĩa trang liệt sĩ, các công trình tưởng niệm và chăm sóc phần mộ liệt sĩ trên địa bàn đóng quân.\r\nNhân dịp này, các đoàn công tác của Sư đoàn đã tổ chức dâng hương, dâng hoa tưởng niệm, thắp nến tri ân tại Công viên Lê Thị Riêng, Nghĩa trang Liệt sĩ TP Hồ Chí Minh, Nghĩa trang Liệt sĩ Củ Chi, Nghĩa trang Liệt sĩ Long Thành (Đồng Nai), Nghĩa trang Liệt sĩ tỉnh Bình Thuận và nhiều nghĩa trang liệt sĩ trên địa bàn các đơn vị đóng quân. Cùng với đó, Sư đoàn tổ chức gặp mặt, tặng quà các đồng chí là thương binh, thân nhân gia đình liệt sĩ đang công tác tại đơn vị; thăm hỏi, tặng quà cán bộ, chiến sĩ đang điều trị tại Bệnh viện Quân y 175 và bệnh xá đơn vị; đồng thời thành lập các đoàn công tác đến thăm hỏi, tặng quà các gia đình chính sách trên địa bàn đóng quân.![image](https://pub-f9c31e10d1244c9cb46f01196c447ad2.r2.dev/news/d11d9e26-a98f-42b3-8b79-655776c242ad/54175f4560364b7a85cf6a572a2ae367.png)Cán bộ, chiến sĩ Trung đoàn 935, Sư đoàn 370 dâng hương tưởng niệm các Anh hùng liệt sĩ tại Nhà bia ghi danh Liệt sĩ ở đơn vị.![image](https://pub-f9c31e10d1244c9cb46f01196c447ad2.r2.dev/news/d11d9e26-a98f-42b3-8b79-655776c242ad/5970b0fe6bea46ff935a1be8566bbc86.png)Cán bộ, chiến sĩ Trung đoàn 917, Sư đoàn 370 thắp nến tri ân các Anh hùng Liệt sĩ tại Nghĩa trang Liệt sĩ Long Tuyền, phường Long Tuyền, TP Cần Thơ.Thông qua chuỗi hoạt động thiết thực, Sư đoàn 370 tiếp tục phát huy đạo lý “Uống nước nhớ nguồn”, “Đền ơn đáp nghĩa” của dân tộc; bồi đắp truyền thống cách mạng, giáo dục cán bộ, chiến sĩ lòng biết ơn sâu sắc đối với các thế hệ cha anh đã anh dũng hy sinh vì sự nghiệp giải phóng dân tộc, xây dựng và bảo vệ Tổ quốc. Đồng thời, góp phần củng cố mối quan hệ đoàn kết quân - dân, xây dựng đơn vị vững mạnh toàn diện, hoàn thành thắng lợi mọi nhiệm vụ được giao.",
                    CategoryId = categories["Tin tức đơn vị"],
                    CoverImageUrl = "https://pub-f9c31e10d1244c9cb46f01196c447ad2.r2.dev/news/d11d9e26-a98f-42b3-8b79-655776c242ad/d1fb09b498f4401e99740c0d7ee5deb0.jpg",
                    AuthorId = authorId,
                    Status = NewsStatus.Draft,
                    ViewCount = 0,
                    CreatedAt = now.AddHours(-3),
                    UpdatedAt = now.AddHours(-1)
                },
                new()
                {
                    Id = Guid.NewGuid(),
                    Title = "Thành phố Hồ Chí Minh tổ chức Lễ truy điệu và an táng 731 hài cốt liệt sĩ được tìm kiếm, quy tập tại Công viên Lê Thị Riêng",
                    Summary = "Sáng 14-10, Đảng bộ, chính quyền, lực lượng vũ trang và nhân dân TP Hồ Chí Minh tổ chức trọng thể Lễ viếng, Lễ truy điệu và an táng 731 hài cốt liệt sĩ được tìm kiếm, quy tập tại Công viên Lê Thị Riêng. Tham dự buổi lễ có đồng chí Lê Minh Hưng - Ủy viên Bộ Chính trị, Thủ tướng Chính phủ nước Cộng hòa xã hội chủ nghĩa Việt Nam; các đồng chí lãnh đạo, nguyên lãnh đạo Đảng, Nhà nước; đại diện lãnh đạo các ban, bộ, ngành Trung ương, Quân đội và địa phương; đại biểu các đơn vị lực lượng vũ trang, cùng đông đảo cán bộ, chiến sĩ và nhân dân.",
                    Content = "> *Thủ tướng Chính phủ Lê Minh Hưng và nguyên Ủy viên Bộ Chính trị, nguyên Chủ tịch nước Nguyễn Minh Triết đưa hài cốt các liệt sĩ rời Công viên Lê Thị Riêng.*\r\n\r\nTừ 7 giờ 30 phút, lễ dâng hoa, dâng hương được tổ chức tại Tượng đài cố Tổng Bí thư Trần Phú, Nhà bia liệt sĩ và các địa điểm trong khuôn viên Công viên Lê Thị Riêng. Đúng 8 giờ, Lễ viếng, Lễ truy điệu chính thức bắt đầu với các nghi thức tưởng niệm, đọc điếu văn và mặc niệm. Sau buổi lễ, 731 hài cốt liệt sĩ được đưa về an táng tại Nghĩa trang Liệt sĩ TP Hồ Chí Minh (phường Long Bình), trong đó có 698 hài cốt được an táng riêng lẻ và 33 hài cốt được an táng chung trong một phần mộ tập thể.\r\n\r\n*Đây là hoạt động có ý nghĩa chính trị, xã hội và nhân văn sâu sắc, thể hiện đạo lý “Uống nước nhớ nguồn”, “Đền ơn đáp nghĩa” của dân tộc; đồng thời góp phần giáo dục truyền thống yêu nước, chủ nghĩa anh hùng cách mạng, bồi đắp niềm tự hào và ý thức trách nhiệm của cán bộ, chiến sĩ và nhân dân, nhất là thế hệ trẻ, đối với sự nghiệp xây dựng và bảo vệ Tổ quốc. Sau bao năm nằm lại, các anh hùng, liệt sĩ đã trở về trong vòng tay quê hương, trong tình cảm tri ân sâu nặng và lòng biết ơn vô hạn của các thế hệ hôm nay và mai sau.*",
                    CategoryId = categories["Tin tức đơn vị"],
                    CoverImageUrl="https://pub-f9c31e10d1244c9cb46f01196c447ad2.r2.dev/news/b5d79d57-84b7-434c-b82e-786e2f9b334e/a731d45b0b1f4d4290a1b24ed830d952.png",
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
                    Title = "Cụm cơ quan Sư đoàn 370 phát động đợt thi đua cao điểm “80 ngày hành động quyết thắng - Viết tiếp bản hùng ca”",
                    Summary = "Sáng 5-10, Cụm cơ quan Sư đoàn 370 tổ chức Lễ phát động đợt thi đua cao điểm với chủ đề “80 ngày thi đua Quyết thắng - Viết tiếp bản hùng ca”, lập thành tích chào mừng kỷ niệm 80 năm Ngày Toàn quốc kháng chiến (19-12-1946 / 19-12-2026), 82 năm Ngày thành lập Quân đội nhân dân Việt Nam (22-12-1944 / 22-12-2026) và 37 năm Ngày hội Quốc phòng toàn dân (22-12-1989 / 22-12-2026). Đại tá Lê Văn Hợi - Bí thư Đảng ủy, Chính ủy Sư đoàn 370, dự và chỉ đạo buổi lễ.",
                    Content = "> Đại tá Lê Văn Hợi - Chính ủy Sư đoàn 370 phát biểu chỉ đạo tại Lễ phát động.\r\n\r\nĐợt thi đua diễn ra trong 80 ngày, từ ngày 5-10 đến hết ngày 25-12-2026, tập trung vào ba nội dung trọng tâm: Đẩy mạnh giáo dục, tuyên truyền, nâng cao nhận thức, trách nhiệm và ý chí quyết tâm cho cán bộ, chiến sĩ; thi đua hoàn thành cao nhất nhiệm vụ chính trị; xây dựng chính quy, chấp hành pháp luật, kỷ luật; đẩy mạnh khoa học, công nghệ, đổi mới sáng tạo, chuyển đổi số và cải cách hành chính.\r\n\r\n![image](https://pub-f9c31e10d1244c9cb46f01196c447ad2.r2.dev/news/b5d79d57-84b7-434c-b82e-786e2f9b334e/be41587725204fbf8f6475aae411e2f3.jpg)Các đại biểu tham dự Lễ phát động thi đua cao điểm.\r\n\r\nTheo đó, Cụm thi đua phấn đấu 100% cán bộ, chiến sĩ có bản lĩnh chính trị vững vàng, sẵn sàng nhận và hoàn thành tốt nhiệm vụ; duy trì nghiêm chế độ sẵn sàng chiến đấu, quản lý vùng trời, quản lý, điều hành bay; tổ chức huấn luyện chặt chẽ, nhất là huấn luyện bay, bảo đảm an toàn tuyệt đối. Phấn đấu không để xảy ra vi phạm pháp luật, kỷ luật và mất an toàn trong các hoạt động. Đồng thời, đẩy mạnh ứng dụng khoa học, công nghệ, chuyển đổi số, phấn đấu 100% văn bản, công việc được giải quyết trên môi trường điện tử.\r\n\r\nThông qua đợt thi đua, Cụm cơ quan Sư đoàn 370 quyết tâm tạo khí thế thi đua sôi nổi, phát huy tinh thần đoàn kết, trách nhiệm, chủ động, sáng tạo của cán bộ, chiến sĩ; phấn đấu hoàn thành thắng lợi nhiệm vụ năm 2026, góp phần xây dựng cơ quan, đơn vị vững mạnh toàn diện “mẫu mực, tiêu biểu”.",
                    CategoryId = categories["Tin tức đơn vị"],
                    AuthorId = authorId,
                    CoverImageUrl="https://pub-f9c31e10d1244c9cb46f01196c447ad2.r2.dev/news/b5d79d57-84b7-434c-b82e-786e2f9b334e/8c8efd8261eb443691b1e03dfbd85d58.jpg",
                    Status = NewsStatus.Published,
                    PublishedAt = now.AddDays(-2),
                    ViewCount = 48,
                    CreatedAt = now.AddDays(-3),
                    UpdatedAt = now.AddDays(-2)
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
            var author = await _context.Users.FirstOrDefaultAsync(u => u.Email == "nhabao1@gmail.com");
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
                Username = "nhabao1",
                Email = "nhabao1@gmail.com",
                PasswordHash = new PasswordHasher().HashPassword("123321@") ?? string.Empty,
                FullName = "Trần Vũ Phương Nam",
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
