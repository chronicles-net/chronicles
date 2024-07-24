namespace Chronicles.Cqrs.Internal;

internal interface ICommandExecutorFactory
{
    ICommandExecutor<TCommand> Create<TCommand>()
        where TCommand : class;
}
