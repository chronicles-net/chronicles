namespace Chronicles.EventStore;

public interface ICommandContext<TCommand>
    where TCommand : class
{
    TCommand Command { get; }

    StreamMetadata Metadata { get; }

    ICommandContext<TCommand> AddEvent(object evt);

    ICommandContext<TCommand> SetResponse(object response);

    TState? GetState<TState>()
        where TState : class;
}
