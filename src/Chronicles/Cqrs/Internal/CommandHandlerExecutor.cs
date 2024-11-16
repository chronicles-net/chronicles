using Chronicles.EventStore;

namespace Chronicles.Cqrs.Internal;

internal class CommandHandlerExecutor<TCommand, THandler>(
    THandler handler)
    : ICommandExecutor<TCommand>
    where TCommand : class
    where THandler : ICommandHandler<TCommand>
{
    public async ValueTask ExecuteAsync(
        IAsyncEnumerable<StreamEvent> events,
        ICommandContext<TCommand> context,
        CancellationToken cancellationToken)
    {
        await foreach (var evt in events)
        {
            handler.ConsumeEvent(
                evt,
                context.Command,
                context.State);
        }

        await handler.ExecuteAsync(context, cancellationToken);
    }
}
