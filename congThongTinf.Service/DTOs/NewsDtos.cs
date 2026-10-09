using congThongTinf.Domain.Entities;

namespace congThongTinf.Service.DTOs;

public class NewsQueryDto
{
    public DateTime? FromDate { get; set; }

    public DateTime? ToDate { get; set; }

    public List<Guid>? CategoryIds { get; set; }
}

public class NewsDto
{
    public Guid Id { get; set; }

    public string Title { get; set; } = string.Empty;

    public string Summary { get; set; } = string.Empty;

    public string Content { get; set; } = string.Empty;

    public string? CoverImageUrl { get; set; }

    public Guid CategoryId { get; set; }

    public string CategoryName { get; set; } = string.Empty;

    public Guid AuthorId { get; set; }

    public string AuthorName { get; set; } = string.Empty;

    public NewsStatus Status { get; set; }

    public DateTime? PublishedAt { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public int ViewCount { get; set; }
}

public class CreateNewsDto
{
    public string Title { get; set; } = string.Empty;

    public string Summary { get; set; } = string.Empty;

    public string Content { get; set; } = string.Empty;

    public string? CoverImageUrl { get; set; }

    public Guid CategoryId { get; set; }
}

public class UpdateNewsDto
{
    public string? Title { get; set; }

    public string? Summary { get; set; }

    public string? Content { get; set; }

    public string? CoverImageUrl { get; set; }

    public Guid? CategoryId { get; set; }
}
