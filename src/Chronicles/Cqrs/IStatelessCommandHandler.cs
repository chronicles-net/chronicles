namespace Chronicles.Cqrs;

public interface IStatelessCommandHandler<TCommand>
    where TCommand : class
{
    ValueTask ExecuteAsync(
        TCommand command,
        ICommandContext<TCommand> context,
        CancellationToken cancellationToken);
}
