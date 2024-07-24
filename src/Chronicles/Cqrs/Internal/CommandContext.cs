using Chronicles.EventStore;

namespace Chronicles.Cqrs.Internal;

internal class CommandContext<TCommand>(
    TCommand command,
    StreamMetadata metadata,
    IStateContext stateContext)
    : ICommandContext<TCommand>
    where TCommand : class
{
    public ICollection<object> Events { get; } = [];

    public TCommand Command { get; } = command;

    public StreamMetadata Metadata { get; set; } = metadata;

    public object? Response { get; set; }

    public IStateContext State { get; } = stateContext;

    public event CommandCompletedAsync<TCommand>? Completed;

    public ICommandContext<TCommand> AddEvent(object evt)
    {
        Events.Add(evt);

        return this;
    }

    internal async ValueTask OnCompleteAsync(
        ICommandCompletionContext<TCommand> context,
        CancellationToken cancellationToken)
    {
        if (Completed != null)
        {
            await Completed.Invoke(context, cancellationToken);
        }
    }
}
