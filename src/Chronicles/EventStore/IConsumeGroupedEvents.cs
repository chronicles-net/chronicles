namespace Chronicles.EventStore;

public interface IConsumeGroupedEvents
{
    void Consume(
        StreamId streamId,
        StreamEvent[] events);
}
