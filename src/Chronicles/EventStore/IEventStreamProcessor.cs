namespace Chronicles.EventStore;

/// <summary>
/// Represents a.
/// </summary>
public interface IEventStreamProcessor
{
    Task ProcessAsync(
        IReadOnlyCollection<StreamEvent> changes,
        CancellationToken cancellationToken);
}