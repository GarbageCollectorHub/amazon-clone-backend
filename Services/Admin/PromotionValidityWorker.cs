namespace AMZN.Services.Admin;

public class PromotionValidityWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<PromotionValidityWorker> _logger;
    private readonly bool _enablePeriodicChecks;

    public PromotionValidityWorker(
        IServiceScopeFactory scopeFactory,
        ILogger<PromotionValidityWorker> logger,
        IConfiguration configuration)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        _enablePeriodicChecks = configuration.GetValue<bool>("Promotions:EnablePeriodicChecks");
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Yield();
        using var timer = new PeriodicTimer(TimeSpan.FromMinutes(1));

        do
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                scope.ServiceProvider.GetRequiredService<AdminPromotionService>().CheckPromotionsValidity();
            }
            catch (Exception exception) when (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogError(exception, "Failed to update promotions");
            }
        }
        while (_enablePeriodicChecks && await timer.WaitForNextTickAsync(stoppingToken));
    }
}
