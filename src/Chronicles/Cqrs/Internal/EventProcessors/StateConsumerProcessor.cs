using Chronicles.EventStore;

namespace Chronicles.Cqrs.Internal.EventProcessors;

internal class StateConsumerProcessor<TConsumer, TState>(
    TConsumer consumer)
    : IEventProcessor
    where TConsumer : class, IStateConsumer<TState>
    where TState : class
{
    public ValueTask ConsumeAsync(
        StreamEvent evt,
        IStateContext state,
        bool hasMore,
        CancellationToken cancellationToken)
        => consumer
            .ConsumeAsync(
                evt,
                state.GetState<TState>(),
                cancellationToken);
}
