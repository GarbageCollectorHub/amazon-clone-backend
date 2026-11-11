using AMZN.Data;
using AMZN.Services.Storage.Cloud;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace AMZN.Tests;

public class SqlImageUploadQuotaTests
{
    private const long MonthlyLimit = 500L * 1024 * 1024;

    // Почти исчерпываем месячный лимит, затем создаём новый экземпляр сервиса с той же БД.
    // Он должен видеть сохранённый счётчик: запрос на один байт сверх лимита отклоняется,
    // ровно последний байт проходит, а с начала следующего UTC-месяца доступен полный объём.
    [LocalDbFact]
    public Task TryReserveAsync_PersistsUsageAndStartsNewUtcMonth() => WithLocalDatabase(async services =>
    {
        var clock = new TestClock(new DateTimeOffset(2026, 12, 31, 23, 59, 59, TimeSpan.Zero));
        var quota = CreateQuota(services, clock);
        Assert.True(await quota.TryReserveAsync(MonthlyLimit - 1));

        var restartedQuota = CreateQuota(services, clock);
        Assert.False(await restartedQuota.TryReserveAsync(2));
        Assert.True(await restartedQuota.TryReserveAsync(1));
        Assert.False(await restartedQuota.TryReserveAsync(1));

        clock.UtcNow = clock.UtcNow.AddSeconds(1);
        Assert.True(await restartedQuota.TryReserveAsync(MonthlyLimit));
        Assert.False(await restartedQuota.TryReserveAsync(1));
    });

    // Два независимых запроса одновременно резервируют по 300 МиБ, когда записи за месяц ещё нет.
    // Каждый проходит отдельно, но вместе они превысили бы лимит. SQL должен принять ровно один,
    // а следующий запрос на оставшиеся 200 МиБ должен пройти: отказ не расходует квоту.
    [LocalDbFact]
    public Task TryReserveAsync_ConcurrentFirstWritesCannotExceedQuota() => WithLocalDatabase(async services =>
    {
        var clock = new TestClock(new DateTimeOffset(2026, 12, 1, 0, 0, 0, TimeSpan.Zero));
        var firstQuota = CreateQuota(services, clock);
        var secondQuota = CreateQuota(services, clock);
        var start = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        async Task<bool> ReserveAfterStart(SqlImageUploadQuota quota)
        {
            await start.Task;
            return await quota.TryReserveAsync(300L * 1024 * 1024);
        }

        var first = ReserveAfterStart(firstQuota);
        var second = ReserveAfterStart(secondQuota);
        start.SetResult();

        Assert.Single(await Task.WhenAll(first, second), accepted => accepted);
        Assert.True(await firstQuota.TryReserveAsync(200L * 1024 * 1024));
        Assert.False(await secondQuota.TryReserveAsync(1));
    });

    private static SqlImageUploadQuota CreateQuota(IServiceScopeFactory scopes, TimeProvider clock)
        => new(scopes, new ConfigurationBuilder().Build(), clock);

    private static async Task WithLocalDatabase(Func<IServiceScopeFactory, Task> test)
    {
        // Never use the app connection string. Each test owns a new LocalDB database and removes only that database.
        string connectionString = $@"Server=(localdb)\MSSQLLocalDB;Database=AmznQuotaTests_{Guid.NewGuid():N};Integrated Security=True;TrustServerCertificate=True;";
        await using var services = new ServiceCollection()
            .AddDbContext<DataContext>(options => options.UseSqlServer(connectionString))
            .BuildServiceProvider();
        await using var scope = services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<DataContext>();

        try
        {
            await db.Database.MigrateAsync();
            await test(services.GetRequiredService<IServiceScopeFactory>());
        }
        finally
        {
            await db.Database.EnsureDeletedAsync();
        }
    }

    private sealed class TestClock(DateTimeOffset utcNow) : TimeProvider
    {
        public DateTimeOffset UtcNow { get; set; } = utcNow;
        public override DateTimeOffset GetUtcNow() => UtcNow;
    }

    private sealed class LocalDbFactAttribute : FactAttribute
    {
        public LocalDbFactAttribute()
        {
            if (Environment.GetEnvironmentVariable("AMZN_TEST_LOCALDB") != "1")
                Skip = "Set AMZN_TEST_LOCALDB=1 to run the quota tests against SQL Server LocalDB.";
        }
    }
}
