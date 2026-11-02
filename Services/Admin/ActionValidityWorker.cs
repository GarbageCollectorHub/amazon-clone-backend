namespace AMZN.Services.Admin;

public class ActionValidityWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<ActionValidityWorker> _logger;

    public ActionValidityWorker(IServiceScopeFactory scopeFactory, ILogger<ActionValidityWorker> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
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
                scope.ServiceProvider.GetRequiredService<AdminActionService>().CheckActionsValidity();
            }
            catch (Exception exception) when (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogError(exception, "Failed to update promotional actions");
            }
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }
}
