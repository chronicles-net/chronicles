namespace Chronicles.Cqrs;

public interface IStatelessCommandHandler<TCommand>
    where TCommand : class
{
    ValueTask ExecuteAsync(
        ICommandContext<TCommand> context,
        CancellationToken cancellationToken);
}
