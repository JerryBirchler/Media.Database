using Media.Common.Helpers;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Media.Database.BackgroundJobs;

/// <summary>
/// Hosted service that processes background work items from the queue.
/// Runs continuously until application shutdown.
/// </summary>
public class BackgroundTaskService : BackgroundService
{
    private readonly IBackgroundTaskQueue _taskQueue;
    private readonly ILogger<BackgroundTaskService> _logger;

    public BackgroundTaskService(
        IBackgroundTaskQueue taskQueue,
        ILogger<BackgroundTaskService> logger)
    {
        _taskQueue = taskQueue;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation(true, "Background Task Service is starting");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var workItem = await _taskQueue.DequeueAsync(stoppingToken);

                await workItem(stoppingToken);
            }
            catch (OperationCanceledException)
            {
                // Expected during shutdown
                _logger.LogInformation(true, "Background Task Service is stopping");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, true, "Error occurred executing background work item");
            }
        }

        _logger.LogInformation(true, "Background Task Service has stopped");
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation(true, "Background Task Service is stopping gracefully");
        await base.StopAsync(cancellationToken);
    }
}
