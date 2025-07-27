using Chronicles.EventStore;

namespace Chronicles.Cqrs.Internal;

internal class StatelessCommandExecutor<TCommand, THandler>(
    THandler handler)
    : ICommandExecutor<TCommand>
    where TCommand : class
    where THandler : IStatelessCommandHandler<TCommand>
{
    public async ValueTask ExecuteAsync(
        IAsyncEnumerable<StreamEvent> events,
        ICommandContext<TCommand> context,
        CancellationToken cancellationToken)
        // Note: The events parameter is not used in stateless command handlers, so no events are read.
        => await handler.ExecuteAsync(context, cancellationToken);
}
