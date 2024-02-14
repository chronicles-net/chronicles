namespace Chronicles.EventStore;

public interface IConsumeGroupedEventsAsync
{
    Task ConsumeAsync(
        StreamId streamId,
        StreamEvent[] events,
        CancellationToken cancellationToken);
}