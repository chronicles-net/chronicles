namespace Chronicles.EventStore;

public interface IConsumeEvent<in TEvent>
{
    void Consume(TEvent evt, EventMetadata metadata);
}

public interface IConsumeEvent<in TEvent, TState>
    : IConsumeEventStateProvider<TState>
{
    TState Consume(
        TEvent evt,
        EventMetadata metadata,
        TState state);
}
