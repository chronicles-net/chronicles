using Chronicles.EventStore;

namespace Chronicles.Cqrs;

public interface IStateConsumer<TState>
{
    ValueTask ConsumeAsync(
        StreamEvent evt,
        TState? state,
        CancellationToken cancellationToken);
}
