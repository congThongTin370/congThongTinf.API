namespace congThongTinf.Service.DTOs;

public class R2StorageUsageDto
{
    public string BucketName { get; set; } = string.Empty;

    public long PayloadBytes { get; set; }

    public long MetadataBytes { get; set; }

    public long UsedBytes { get; set; }

    public long ObjectCount { get; set; }

    public long QuotaBytes { get; set; }

    public long RemainingBytes { get; set; }

    public double UsedPercent { get; set; }

    public DateTime? MeasuredAt { get; set; }
}
