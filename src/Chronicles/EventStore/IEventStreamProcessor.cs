namespace Chronicles.EventStore;

/// <summary>
/// Defines a contract for processing event streams.
/// </summary>
public interface IEventStreamProcessor
{
    /// <summary>
    /// Processes a collection of stream events asynchronously.
    /// </summary>
    /// <param name="changes">The collection of stream events to process.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    Task ProcessAsync(
        IReadOnlyCollection<StreamEvent> changes,
        CancellationToken cancellationToken);
}
