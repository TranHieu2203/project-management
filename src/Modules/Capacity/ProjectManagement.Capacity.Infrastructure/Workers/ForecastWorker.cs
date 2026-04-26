using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ProjectManagement.Capacity.Application.Commands.TriggerForecastCompute;

namespace ProjectManagement.Capacity.Infrastructure.Workers;

public sealed class ForecastWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<ForecastWorker> _logger;

    public ForecastWorker(IServiceScopeFactory scopeFactory, ILogger<ForecastWorker> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("[ForecastWorker] Started — runs every Monday 07:00 UTC");

        while (!stoppingToken.IsCancellationRequested)
        {
            var delay = GetDelayUntilNextMonday();
            _logger.LogInformation("[ForecastWorker] Next run in {Hours:F1}h", delay.TotalHours);

            try
            {
                await Task.Delay(delay, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }

            await RunForecastAsync(stoppingToken);
        }
    }

    private async Task RunForecastAsync(CancellationToken ct)
    {
        _logger.LogInformation("[ForecastWorker] Running global forecast compute…");
        try
        {
            await using var scope = _scopeFactory.CreateAsyncScope();
            var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

            // Guid.Empty = system-level — includes all active projects
            var result = await mediator.Send(new TriggerForecastComputeCommand(Guid.Empty), ct);
            _logger.LogInformation("[ForecastWorker] Forecast v{Version} completed: {Status}", result.Version, result.Status);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "[ForecastWorker] Forecast compute failed");
        }
    }

    private static TimeSpan GetDelayUntilNextMonday()
    {
        var now = DateTime.UtcNow;
        var daysUntilMonday = ((int)DayOfWeek.Monday - (int)now.DayOfWeek + 7) % 7;
        if (daysUntilMonday == 0 && now.Hour >= 7) daysUntilMonday = 7;

        var nextRun = now.Date.AddDays(daysUntilMonday).AddHours(7);
        var delay = nextRun - now;
        return delay > TimeSpan.Zero ? delay : TimeSpan.FromDays(7);
    }
}
