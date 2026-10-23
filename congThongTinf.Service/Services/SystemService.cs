using System.Text.RegularExpressions;
using congThongTinf.Domain;
using congThongTinf.Domain.Entities;
using congThongTinf.Service.DTOs;
using congThongTinf.Service.Interfaces;
using congThongTinf.Service.Interfaces.Commons;
using congThongTinf.Service.Utils;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace congThongTinf.Service.Services;

public class SystemService : ISystemService
{
    private static readonly Regex MarkdownImage = new(@"!\[[^\]]*\]\([^)]+\)", RegexOptions.Compiled);

    private readonly congThongTinfDbContext _context;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IBlobService _blobService;
    private readonly ILoggerService _logger;
    private readonly IHostEnvironment _environment;
    private readonly IConfiguration _configuration;

    public SystemService(
        congThongTinfDbContext context,
        IUnitOfWork unitOfWork,
        IBlobService blobService,
        ILoggerService logger,
        IHostEnvironment environment,
        IConfiguration configuration)
    {
        _context = context;
        _unitOfWork = unitOfWork;
        _blobService = blobService;
        _logger = logger;
        _environment = environment;
        _configuration = configuration;
    }

    public async Task<SeedAllResultDto> SeedAllAsync(CancellationToken cancellationToken = default)
    {
        await ClearDatabaseAsync();
        var usersRoles = await SeedUsersRolesAsync();
        var newsCount = await SeedNewsAsync(cancellationToken);
        var leaderCount = await SeedLeadersAsync();
        var bannerCount = await SeedBannersAsync(cancellationToken);
        return new SeedAllResultDto
        {
            RoleCount = usersRoles.RoleCount,
            AdminCount = usersRoles.AdminCount,
            JournalistCount = usersRoles.JournalistCount,
            NewsCount = newsCount,
            LeaderCount = leaderCount,
            BannerCount = bannerCount
        };
    }

    public async Task ClearDatabaseAsync()
    {
        var strategy = _context.Database.CreateExecutionStrategy();
        await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                _logger.Info("Bắt đầu xóa dữ liệu trong database...");
                await _context.News.ExecuteDeleteAsync();
                await _context.Leaders.ExecuteDeleteAsync();
                await _context.Banners.ExecuteDeleteAsync();
                await _context.Documents.ExecuteDeleteAsync();
                await _context.SharedFiles.ExecuteDeleteAsync();
                await _context.Users.ExecuteDeleteAsync();
                await _context.Roles.ExecuteDeleteAsync();
                await _context.Categories.ExecuteDeleteAsync();
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

    public async Task<SeedUsersRolesResultDto> SeedUsersRolesAsync()
    {
        var now = DateTime.UtcNow;
        var roles = new (RoleType Name, string Description)[]
        {
            (RoleType.Admin, "Xem thống kê, quản lý tài khoản và vai trò, quản lý văn bản nội bộ."),
            (RoleType.Journalist, "Tạo, sửa, xuất bản và quản lý bài viết của mình.")
        };

        var names = roles.Select(r => r.Name).ToArray();
        var existing = await _unitOfWork.Roles.GetQueryable()
            .Where(r => names.Contains(r.Name))
            .Select(r => r.Name)
            .ToListAsync();

        var addedRoles = 0;
        foreach (var item in roles)
        {
            if (existing.Contains(item.Name))
                continue;

            await _unitOfWork.Roles.AddAsync(new Role
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
            await _unitOfWork.SaveChangesAsync();

        var adminRole = await _unitOfWork.Roles.FirstOrDefaultAsync(r => r.Name == RoleType.Admin)
            ?? throw ErrorHelper.Internal("Không tìm thấy vai trò Admin sau khi seed.");
        var journalistRole = await _unitOfWork.Roles.FirstOrDefaultAsync(r => r.Name == RoleType.Journalist)
            ?? throw ErrorHelper.Internal("Không tìm thấy vai trò Journalist sau khi seed.");
        var hasher = new PasswordHasher();

        var addedAdmins = await SeedUsers(
            now,
            adminRole.Id,
            hasher,
            "123321@",
            [
                ("admin", "admin1@gmail.com", "Quản trị viên 1"),
                ("admin2", "admin2@gmail.com", "Quản trị viên 2")
            ]);

        var addedJournalists = await SeedUsers(
            now,
            journalistRole.Id,
            hasher,
            "123321@",
            [
                ("nhabao1", "nhabao1@gmail.com", "Trần Vũ Phương Nam"),
                ("nhabao2", "nhabao2@gmail.com", "Nhà báo 2")
            ]);

        _logger.Success($"Đã seed {addedRoles} vai trò, {addedAdmins} tài khoản admin và {addedJournalists} tài khoản phóng viên.");
        return new SeedUsersRolesResultDto
        {
            RoleCount = addedRoles,
            AdminCount = addedAdmins,
            JournalistCount = addedJournalists
        };
    }

    public async Task<int> SeedLeadersAsync()
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
        var existing = await _unitOfWork.Leaders.GetQueryable()
            .Where(l => names.Contains(l.FullName))
            .Select(l => l.FullName)
            .ToListAsync();

        var added = 0;
        foreach (var leader in leaders)
        {
            if (existing.Contains(leader.FullName))
                continue;

            await _unitOfWork.Leaders.AddAsync(new Leader
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
            await _unitOfWork.SaveChangesAsync();

        _logger.Success($"Đã seed {added} lãnh đạo đơn vị.");
        return added;
    }

    public async Task<int> SeedBannersAsync(CancellationToken cancellationToken = default)
    {
        var admin = await _unitOfWork.Users.FirstOrDefaultAsync(u => u.Email == "admin1@gmail.com")
            ?? await _unitOfWork.Users.FirstOrDefaultAsync(u => u.IsActive);
        if (admin == null)
        {
            _logger.Warn("Không seed banner: chưa có tài khoản admin.");
            return 0;
        }

        var bannersDir = Path.Combine(ResolveImageRoot(), "banners");
        if (!Directory.Exists(bannersDir))
        {
            _logger.Warn($"Không seed banner: thiếu thư mục {bannersDir}.");
            return 0;
        }

        var seeds = new (string FileStem, string Title, BannerPosition Position)[]
        {
            ("top-banner", "Banner đầu trang", BannerPosition.Header),
            ("mid-banner1", "Banner giữa trang chủ", BannerPosition.HomeMid)
        };

        var existingKeys = await _unitOfWork.Banners.GetQueryable()
            .Select(b => b.FileKey)
            .ToListAsync(cancellationToken);

        var now = DateTime.UtcNow;
        var added = 0;

        foreach (var seed in seeds)
        {
            var localPath = FindImageByStem(bannersDir, seed.FileStem);
            if (localPath == null)
            {
                _logger.Warn($"Bỏ qua banner '{seed.FileStem}': chưa có file trong {bannersDir}.");
                continue;
            }

            var upload = await UploadLocalImageAsync(localPath, "banners", Path.GetFileName(localPath), cancellationToken);
            if (existingKeys.Contains(upload.Key))
                continue;

            await _unitOfWork.Banners.AddAsync(new Banner
            {
                Id = Guid.NewGuid(),
                Title = seed.Title,
                ImageUrl = upload.Url,
                FileKey = upload.Key,
                FileName = Path.GetFileName(upload.Key),
                FileType = upload.ContentType,
                FileSize = upload.Size,
                Position = seed.Position,
                IsActive = true,
                UploadedByUserId = admin.Id,
                CreatedAt = now,
                UpdatedAt = now
            });
            added++;
        }

        if (added > 0)
            await _unitOfWork.SaveChangesAsync();

        _logger.Success($"Đã seed {added} banner hệ thống từ thư mục local.");
        return added;
    }

    public async Task<int> SeedNewsAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var categories = await EnsureCategories(now);
        var authorId = await EnsureJournalist(now);
        var imageRoot = ResolveImageRoot();

        var articles = new List<News>();
        foreach (var def in SeedNewsCatalog.Articles)
        {
            var articleDir = Path.Combine(imageRoot, "news", def.Folder);
            if (!Directory.Exists(articleDir))
                throw ErrorHelper.BadRequest($"Không tìm thấy thư mục ảnh seed '{articleDir}'.");

            var newsId = Guid.NewGuid();
            var folder = $"news/{authorId}/{newsId:N}";
            var (coverUrl, content) = await UploadArticleImagesAsync(articleDir, folder, def, cancellationToken);

            var createdAt = now.Add(def.CreatedOffset);
            var updatedAt = now.Add(def.UpdatedOffset);
            articles.Add(new News
            {
                Id = newsId,
                Title = def.Title,
                Summary = def.Summary,
                Content = content,
                CoverImageUrl = coverUrl,
                CategoryId = categories[def.CategoryName],
                AuthorId = authorId,
                Status = def.Status,
                ViewCount = def.ViewCount,
                PublishedAt = def.PublishedOffset is { } published
                    ? now.Add(published)
                    : def.Status == NewsStatus.Published ? createdAt : null,
                CreatedAt = createdAt,
                UpdatedAt = updatedAt
            });
        }

        await _unitOfWork.News.AddRangeAsync(articles);
        await _unitOfWork.SaveChangesAsync();
        _logger.Success($"Đã seed {articles.Count} bài viết (ảnh đã upload lên kho lưu trữ).");
        return articles.Count;
    }

    private async Task<(string CoverUrl, string Content)> UploadArticleImagesAsync(
        string articleDir,
        string storageFolder,
        SeedNewsCatalog.Article def,
        CancellationToken cancellationToken)
    {
        var coverPath = FindImageByStem(articleDir, "cover")
            ?? throw ErrorHelper.BadRequest($"Thiếu ảnh bìa cover.* trong '{articleDir}' ({def.Title}).");

        var cover = await UploadLocalImageAsync(coverPath, storageFolder, "cover.webp", cancellationToken);

        var contentFiles = Directory.GetFiles(articleDir)
            .Where(path => ImageFiles.HasImageExtension(path))
            .Select(path => new FileInfo(path))
            .Where(file => Regex.IsMatch(file.Name, @"^\d+\.", RegexOptions.IgnoreCase))
            .OrderBy(file => file.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();

        var placeholderCount = MarkdownImage.Matches(def.Content).Count;
        if (contentFiles.Count < placeholderCount)
        {
            throw ErrorHelper.BadRequest(
                $"Bài '{def.Title}' cần {placeholderCount} ảnh nội dung (01.*, 02.*, …) nhưng chỉ có {contentFiles.Count} file trong '{articleDir}'.");
        }

        var uploadedUrls = new List<string>(placeholderCount);
        for (var i = 0; i < placeholderCount; i++)
        {
            var objectName = $"{i + 1:00}.webp";
            var uploaded = await UploadLocalImageAsync(contentFiles[i].FullName, storageFolder, objectName, cancellationToken);
            uploadedUrls.Add(uploaded.Url);
        }

        var index = 0;
        var content = MarkdownImage.Replace(def.Content, _ =>
        {
            var url = uploadedUrls[index];
            index++;
            return $"![image]({url})";
        });

        return (cover.Url, content);
    }

    private async Task<UploadFileResultDto> UploadLocalImageAsync(
        string path,
        string folder,
        string objectFileName,
        CancellationToken cancellationToken)
    {
        await using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
        var file = new FormFile(stream, 0, stream.Length, "file", Path.GetFileName(path))
        {
            Headers = new HeaderDictionary(),
            ContentType = "image/webp"
        };

        var result = await _blobService.UploadFileAsync(file, folder, cancellationToken, objectFileName);
        _logger.Info($"Seed uploaded '{path}' -> {result.Key}");
        return result;
    }

    private string ResolveImageRoot()
    {
        var configured = _configuration["Seed:ImageRoot"];
        var candidates = new List<string>();
        if (!string.IsNullOrWhiteSpace(configured))
        {
            candidates.Add(Path.IsPathRooted(configured)
                ? configured
                : Path.GetFullPath(Path.Combine(_environment.ContentRootPath, configured)));
        }

        candidates.Add(Path.GetFullPath(Path.Combine(_environment.ContentRootPath, "seed", "images")));
        candidates.Add(Path.GetFullPath(Path.Combine(_environment.ContentRootPath, "..", "seed", "images")));
        candidates.Add(Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "seed", "images")));
        candidates.Add(Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "..", "seed", "images")));

        var match = candidates.FirstOrDefault(Directory.Exists);
        if (match == null)
            throw ErrorHelper.Internal("Không tìm thấy thư mục seed/images. Đặt ảnh seed hoặc cấu hình Seed:ImageRoot.");

        return match;
    }

    private static string? FindImageByStem(string directory, string stem)
    {
        return Directory.GetFiles(directory)
            .Where(ImageFiles.HasImageExtension)
            .FirstOrDefault(path =>
                string.Equals(Path.GetFileNameWithoutExtension(path), stem, StringComparison.OrdinalIgnoreCase));
    }

    private async Task<int> SeedUsers(
        DateTime now,
        Guid roleId,
        PasswordHasher hasher,
        string password,
        (string Username, string Email, string FullName)[] users)
    {
        var emails = users.Select(u => u.Email).ToArray();
        var existingEmails = await _unitOfWork.Users.GetQueryable()
            .Where(u => emails.Contains(u.Email))
            .Select(u => u.Email)
            .ToListAsync();

        var added = 0;
        foreach (var item in users)
        {
            if (existingEmails.Contains(item.Email))
                continue;

            await _unitOfWork.Users.AddAsync(new User
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
            await _unitOfWork.SaveChangesAsync();

        return added;
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
        var existing = await _unitOfWork.Categories.GetQueryable()
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
            await _unitOfWork.Categories.AddAsync(category);
            existing.Add(category);
        }

        await _unitOfWork.SaveChangesAsync();
        return existing.ToDictionary(c => c.Name, c => c.Id);
    }

    private async Task<Guid> EnsureJournalist(DateTime now)
    {
        var author = await _unitOfWork.Users.FirstOrDefaultAsync(u => u.Email == "nhabao1@gmail.com");
        if (author != null)
            return author.Id;

        var role = await _unitOfWork.Roles.FirstOrDefaultAsync(r => r.Name == RoleType.Journalist);
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
            await _unitOfWork.Roles.AddAsync(role);
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
        await _unitOfWork.Users.AddAsync(author);
        await _unitOfWork.SaveChangesAsync();
        return author.Id;
    }
}
