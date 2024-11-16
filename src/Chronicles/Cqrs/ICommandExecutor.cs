using Chronicles.EventStore;

namespace Chronicles.Cqrs;

public interface ICommandExecutor<TCommand>
    where TCommand : class
{
    ValueTask ExecuteAsync(
        IAsyncEnumerable<StreamEvent> events,
        ICommandContext<TCommand> context,
        CancellationToken cancellationToken);
}