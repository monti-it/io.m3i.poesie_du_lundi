using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using PoesieDuLundi.Application;

namespace PoesieDuLundi.Infrastructure;

/// <summary>
/// The materialising job's recurring trigger (docs/ARCHITECTURE.md "Publishing model") — a thin
/// timer loop around <see cref="MaterialiseDuePoems"/>, which does the actual work and is
/// unit-tested on its own. One DI scope per sweep since <see cref="IPoemRepository"/> is scoped.
/// </summary>
public sealed class PoemPublicationBackgroundService(
    IHostEnvironment environment,
    IServiceScopeFactory scopeFactory,
    TimeProvider timeProvider,
    PublicationJobOptions options,
    ILogger<PoemPublicationBackgroundService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!PublicationJobStartupPolicy.ShouldRun(environment))
        {
            return;
        }

        using var timer = new PeriodicTimer(options.Interval, timeProvider);

        do
        {
            try
            {
                await using var scope = scopeFactory.CreateAsyncScope();
                var materialise = scope.ServiceProvider.GetRequiredService<MaterialiseDuePoems>();
                var published = await materialise.HandleAsync(stoppingToken);

                if (published > 0)
                {
                    logger.LogInformation("Materialised {Count} scheduled poem(s) to Published.", published);
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, "Publication materialisation sweep failed.");
            }
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }
}
