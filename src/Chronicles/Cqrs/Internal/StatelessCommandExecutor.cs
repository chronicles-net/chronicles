using Chronicles.EventStore;

namespace Chronicles.Cqrs.Internal;

internal class StatelessCommandExecutor<TCommand, THandler>(
    THandler handler)
    : ICommandExecutor<TCommand>
    where TCommand : class
    where THandler : IStatelessCommandHandler<TCommand>
{
    public async ValueTask ExecuteAsync(
        TCommand command,
        IAsyncEnumerable<StreamEvent> events,
        ICommandContext<TCommand> context,
        CancellationToken cancellationToken)
        => await handler.ExecuteAsync(command, context, cancellationToken);
}
