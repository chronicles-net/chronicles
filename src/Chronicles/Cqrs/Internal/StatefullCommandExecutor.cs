using Chronicles.EventStore;

namespace Chronicles.Cqrs.Internal;

internal class StatefulCommandExecutor<TCommand, THandler, TState>(
    THandler handler)
    : ICommandExecutor<TCommand>
    where TCommand : class
    where THandler : ICommandHandler<TCommand, TState>
    where TState : class
{
    public async ValueTask ExecuteAsync(
        TCommand command,
        IAsyncEnumerable<StreamEvent> events,
        ICommandContext<TCommand> context,
        CancellationToken cancellationToken)
    {
        var state = handler.CreateState(context.Metadata.StreamId);

        await foreach (var evt in events)
        {
            state = handler.ConsumeEvent(evt, state);
        }

        context.State.SetState(state);

        await handler.ExecuteAsync(context, state, cancellationToken);
    }
}
