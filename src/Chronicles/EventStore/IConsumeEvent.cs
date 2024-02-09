namespace Chronicles.EventStore;

public interface IConsumeEvent<in TEvent>
{
    void Consume(TEvent evt, EventMetadata metadata);
}