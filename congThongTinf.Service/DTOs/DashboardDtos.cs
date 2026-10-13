using congThongTinf.Domain.Entities;

namespace congThongTinf.Service.DTOs;

public class DashboardQueryDto
{
    public DateTime? FromDate { get; set; }

    public DateTime? ToDate { get; set; }
}

public class DashboardNewsSectionDto
{
    public DateTime FromDate { get; set; }

    public DateTime ToDate { get; set; }

    public DashboardNewsStatsDto Stats { get; set; } = new();

    public List<DashboardCategoryCountDto> NewsByCategory { get; set; } = new();

    public List<DashboardCountByDayDto> NewsByDay { get; set; } = new();

    public List<DashboardNewsPreviewDto> PendingNews { get; set; } = new();

    public List<DashboardNewsPreviewDto> TopViewedNews { get; set; } = new();
}

public class DashboardUserStatsDto
{
    public int Total { get; set; }

    public int Active { get; set; }

    public int Inactive { get; set; }

    public int Admin { get; set; }

    public int Journalist { get; set; }

    public int LoggedInInRange { get; set; }
}

public class DashboardNewsStatsDto
{
    public int Total { get; set; }

    public int Draft { get; set; }

    public int Pending { get; set; }

    public int Published { get; set; }

    public int Archived { get; set; }

    public int CreatedInRange { get; set; }

    public int PublishedInRange { get; set; }

    public int TotalViews { get; set; }

    public List<DashboardCategoryCountDto> ByCategory { get; set; } = new();

    public List<DashboardCountByDayDto> ByDay { get; set; } = new();

    public List<DashboardNewsPreviewDto> PendingItems { get; set; } = new();

    public List<DashboardNewsPreviewDto> TopViewedItems { get; set; } = new();
}

public class DashboardDocumentStatsDto
{
    public int Total { get; set; }

    public int UploadedInRange { get; set; }

    public long TotalFileSize { get; set; }

    public List<DashboardDocumentCategoryCountDto> ByCategory { get; set; } = new();

    public List<DashboardDocumentPreviewDto> RecentItems { get; set; } = new();
}

public class DashboardCategoryStatsDto
{
    public int Total { get; set; }

    public int Active { get; set; }
}

public class DashboardBannerStatsDto
{
    public int Total { get; set; }

    public int Active { get; set; }
}

public class DashboardCategoryCountDto
{
    public Guid CategoryId { get; set; }

    public string CategoryName { get; set; } = string.Empty;

    public int Count { get; set; }
}

public class DashboardDocumentCategoryCountDto
{
    public DocumentCategory Category { get; set; }

    public int Count { get; set; }
}

public class DashboardCountByDayDto
{
    public DateTime Date { get; set; }

    public int Created { get; set; }

    public int Published { get; set; }
}

public class DashboardNewsPreviewDto
{
    public Guid Id { get; set; }

    public string Title { get; set; } = string.Empty;

    public string AuthorName { get; set; } = string.Empty;

    public string CategoryName { get; set; } = string.Empty;

    public int ViewCount { get; set; }

    public DateTime CreatedAt { get; set; }
}

public class DashboardDocumentPreviewDto
{
    public Guid Id { get; set; }

    public string Title { get; set; } = string.Empty;

    public string FileName { get; set; } = string.Empty;

    public DocumentCategory Category { get; set; }

    public DateTime CreatedAt { get; set; }
}
