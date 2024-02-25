namespace Chronicles.EventStore.Internal.Commands;

public interface ICommandProcessor<TCommand>
    where TCommand : class
{
    ValueTask<CommandResult> ExecuteAsync(
        TCommand command,
        StreamId streamId,
        string? storeName,
        CancellationToken cancellationToken);
}