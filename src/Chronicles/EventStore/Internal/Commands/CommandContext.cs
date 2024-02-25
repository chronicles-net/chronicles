using Chronicles.EventStore.Internal.EventConsumers;

namespace Chronicles.EventStore.Internal.Commands;

public class CommandContext<TCommand>(
    StreamMetadata metadata,
    EventConsumerStateContext stateContext,
    TCommand command)
    : ICommandContext<TCommand>
    where TCommand : class
{
    public ICollection<object> Events { get; } = [];

    public TCommand Command { get; } = command;

    public StreamMetadata Metadata => metadata;

    public object? ResponseObject { get; private set; }

    public TState? GetState<TState>()
        where TState : class
        => stateContext.GetState<TState>();

    public ICommandContext<TCommand> AddEvent(object evt)
    {
        Events.Add(evt);

        return this;
    }

    public ICommandContext<TCommand> SetResponse(
        object response)
    {
        ResponseObject = response;
        return this;
    }
}