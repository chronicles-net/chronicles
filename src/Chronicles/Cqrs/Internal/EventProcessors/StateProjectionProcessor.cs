using Chronicles.EventStore;

namespace Chronicles.Cqrs.Internal.EventProcessors;

internal class StateProjectionProcessor<TConsumer, TState>(
    TConsumer consumer)
    : IEventProcessor
    where TState : class
    where TConsumer : class, IStateProjection<TState>
{
    public async ValueTask ConsumeAsync(
        StreamEvent evt,
        IStateContext state,
        bool hasMore,
        CancellationToken cancellationToken)
    {
        var document = await GetStateAsync(
            evt,
            state,
            cancellationToken);

        document = consumer.ConsumeEvent(evt, document);

        if (!hasMore)
        {
            await CommitAsync(document, cancellationToken);
        }
    }

    protected virtual Task CommitAsync(
        TState state,
        CancellationToken cancellationToken)
        => Task.CompletedTask;

    protected virtual Task<TState> GetStateAsync(
        StreamEvent evt,
        IStateContext state,
        CancellationToken cancellationToken)
        => Task.FromResult(
            state.GetState<TState>() ?? consumer.CreateState(evt.Metadata.StreamId));
}