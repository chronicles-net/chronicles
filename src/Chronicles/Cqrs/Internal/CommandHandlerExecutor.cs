using Chronicles.EventStore;

namespace Chronicles.Cqrs.Internal;

internal class CommandHandlerExecutor<TCommand, THandler>(
    THandler handler)
    : ICommandExecutor<TCommand>
    where TCommand : class
    where THandler : ICommandHandler<TCommand>
{
    public async ValueTask ExecuteAsync(
        TCommand command,
        IAsyncEnumerable<StreamEvent> events,
        ICommandContext<TCommand> context,
        CancellationToken cancellationToken)
    {
        await foreach (var evt in events)
        {
            handler.ConsumeEvent(
                evt,
                command,
                context.State);
        }

        await handler.ExecuteAsync(context, cancellationToken);
    }
}
