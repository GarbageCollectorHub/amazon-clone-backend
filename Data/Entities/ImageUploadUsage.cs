namespace AMZN.Data.Entities;

public class ImageUploadUsage
{
    // UTC month in YYYYMM format; each month has its own persistent counter.
    public int Month { get; set; }
    public long ReservedBytes { get; set; }
}
