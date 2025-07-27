namespace Chronicles.Documents;

/// <summary>
/// Provides methods to manage the lifecycle of a document subscription service.
/// </summary>
public interface ISubscriptionService
{
    /// <summary>
    /// Starts the subscription service asynchronously.
    /// </summary>
    /// <param name="cancellationToken">A token to cancel the start operation.</param>
    /// <returns>A task representing the asynchronous start operation.</returns>
    Task StartAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Stops the subscription service asynchronously.
    /// </summary>
    /// <param name="cancellationToken">A token to cancel the stop operation.</param>
    /// <returns>A task representing the asynchronous stop operation.</returns>
    Task StopAsync(CancellationToken cancellationToken);
}
