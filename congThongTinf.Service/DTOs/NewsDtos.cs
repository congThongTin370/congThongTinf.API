using System.ComponentModel;
using congThongTinf.Domain.Entities;
using Microsoft.AspNetCore.Http;

namespace congThongTinf.Service.DTOs;

public class NewsQueryDto
{
    public int PageIndex { get; set; } = 1;

    public int PageSize { get; set; } = 10;

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
    [DefaultValue("Sư đoàn 370 tổ chức hội nghị giao ban tháng 10")]
    public string Title { get; set; } = string.Empty;

    [DefaultValue("Các đơn vị báo cáo kết quả huấn luyện, sẵn sàng chiến đấu và công tác bảo đảm trong tháng.")]
    public string Summary { get; set; } = string.Empty;

    [DefaultValue("Trong bối cảnh nhiệm vụ huấn luyện và sẵn sàng chiến đấu ngày càng được đặt ra cao hơn, các đơn vị thuộc Sư đoàn Không quân 370 tiếp tục đẩy mạnh công tác chuẩn bị, bảo đảm đầy đủ về con người, trang bị và điều kiện thực hiện nhiệm vụ. Hội nghị giao ban đã đánh giá kết quả huấn luyện trong tháng, nêu rõ những ưu điểm và các tồn tại cần khắc phục ngay.")]
    public string Content { get; set; } = string.Empty;

    /// <summary>
    ///     Ảnh bìa tải từ máy local. Được upload lên bucket, URL trả về sẽ được lưu vào CoverImageUrl.
    /// </summary>
    public IFormFile? CoverImage { get; set; }

    [DefaultValue("00000000-0000-0000-0000-000000000000")]
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

public class NewsImageDto
{
    public string Url { get; set; } = string.Empty;
}
