namespace Chronicles.EventStore;

public interface IConsumeEventAsync<in TEvent>
{
    Task ConsumeAsync(
        TEvent evt,
        EventMetadata metadata,
        CancellationToken cancellationToken);
}
