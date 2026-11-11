namespace AMZN.Services.Storage.Cloud;

public interface IImageUploadQuota
{
    Task<bool> TryReserveAsync(long bytes, CancellationToken cancellationToken = default);
}
