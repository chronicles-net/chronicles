namespace Chronicles.EventStore.Internal.Commands;

public interface ICommandHandlerFactory
{
    ICommandHandler<TCommand> Create<TCommand>()
        where TCommand : class;
}