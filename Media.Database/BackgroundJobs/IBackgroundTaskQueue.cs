namespace Media.Database.BackgroundJobs;

/// <summary>
/// Queue for background work items that need to execute asynchronously.
/// </summary>
public interface IBackgroundTaskQueue
{
    /// <summary>
    /// Adds a work item to the queue.
    /// </summary>
    ValueTask QueueBackgroundWorkItemAsync(Func<CancellationToken, ValueTask> workItem);

    /// <summary>
    /// Dequeues a work item. Blocks until work is available.
    /// </summary>
    ValueTask<Func<CancellationToken, ValueTask>> DequeueAsync(CancellationToken cancellationToken);
}
