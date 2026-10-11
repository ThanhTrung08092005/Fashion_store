using Fashion_store.Data;
using Microsoft.EntityFrameworkCore;

namespace Fashion_store.Services;

public sealed class SurveyExpirationService : BackgroundService
{
    private static readonly TimeSpan CheckInterval = TimeSpan.FromSeconds(30);
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<SurveyExpirationService> _logger;

    public SurveyExpirationService(IServiceScopeFactory scopeFactory, ILogger<SurveyExpirationService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public static Task<int> CloseExpiredSurveysAsync(
        FashionStoreDbContext context,
        CancellationToken cancellationToken = default)
    {
        var now = DateTime.Now;
        return context.KhaoSat
            .Where(survey => survey.TrangThai && survey.NgayKetThuc < now)
            .ExecuteUpdateAsync(update => update.SetProperty(survey => survey.TrangThai, false), cancellationToken);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(CheckInterval);
        do
        {
            try
            {
                await using var scope = _scopeFactory.CreateAsyncScope();
                var context = scope.ServiceProvider.GetRequiredService<FashionStoreDbContext>();
                var closedCount = await CloseExpiredSurveysAsync(context, stoppingToken);
                if (closedCount > 0)
                    _logger.LogInformation("Automatically closed {SurveyCount} expired surveys.", closedCount);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                _logger.LogWarning(exception, "Could not close expired surveys; will retry at the next interval.");
            }
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }
}
