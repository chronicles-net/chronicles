using Chronicles.EventStore;

namespace Chronicles.Cqrs;

public interface IStateProjection<TState>
{
    TState CreateState(
        StreamId streamId);

    TState ConsumeEvent(
        StreamEvent evt,
        TState state);
}
