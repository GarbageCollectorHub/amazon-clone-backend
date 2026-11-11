using AMZN.Data;
using AMZN.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace AMZN.Services.Storage.Cloud;

public class SqlImageUploadQuota(
    IServiceScopeFactory scopeFactory, IConfiguration configuration, TimeProvider timeProvider) : IImageUploadQuota
{
    public async Task<bool> TryReserveAsync(long bytes, CancellationToken cancellationToken = default)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(bytes);
        long limitMiB = configuration.GetValue<long>("ImageUploads:MonthlyLimitMiB", 500);
        if (limitMiB <= 0 || limitMiB > long.MaxValue / (1024 * 1024))
            throw new InvalidOperationException("ImageUploads:MonthlyLimitMiB must be a positive size.");
        long limitBytes = limitMiB * 1024 * 1024;
        if (bytes > limitBytes) return false;

        var now = timeProvider.GetUtcNow();
        int month = now.Year * 100 + now.Month;

        // Use a separate context: rolling back a product must not restore its upload allowance.
        await using var scope = scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<DataContext>();
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);

        // Lock the month (including a missing row) until commit, so concurrent uploads cannot exceed the limit.
        var usage = await db.ImageUploadUsage
            .FromSqlInterpolated($"SELECT * FROM [AMZN].[ImageUploadUsage] WITH (UPDLOCK, HOLDLOCK) WHERE [Month] = {month}")
            .SingleOrDefaultAsync(cancellationToken);
        if (usage == null)
        {
            usage = new ImageUploadUsage { Month = month };
            db.ImageUploadUsage.Add(usage);
        }

        if (bytes > limitBytes - usage.ReservedBytes) return false;

        usage.ReservedBytes += bytes;
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return true;
    }
}
