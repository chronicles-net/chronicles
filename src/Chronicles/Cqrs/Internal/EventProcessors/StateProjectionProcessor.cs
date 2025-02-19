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

        var mutatedState = consumer.ConsumeEvent(evt, document);
        if (mutatedState != null)
        {
            state.SetState(mutatedState);
            state.SetState(mutatedState, "state-changed");
        }

        if (!hasMore)
        {
            var isChanged = state.GetState<TState>("state-changed") != null;
            await CommitAsync(
                mutatedState ?? document,
                isChanged,
                state,
                cancellationToken);
        }
    }

    protected virtual Task CommitAsync(
        TState document,
        bool isChanged,
        IStateContext state,
        CancellationToken cancellationToken)
        => Task.CompletedTask;

    protected virtual Task<TState> GetStateAsync(
        StreamEvent evt,
        IStateContext state,
        CancellationToken cancellationToken)
        => Task.FromResult(
            state.GetState<TState>() ?? consumer.CreateState(evt.Metadata.StreamId));
}
