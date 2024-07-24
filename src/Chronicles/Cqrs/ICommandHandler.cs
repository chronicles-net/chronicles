using Chronicles.EventStore;

namespace Chronicles.Cqrs;

public interface ICommandHandler<TCommand, TState>
    : IStateProjection<TState>
    where TCommand : class
    where TState : class
{
    ValueTask ExecuteAsync(
        ICommandContext<TCommand> context,
        TState state,
        CancellationToken cancellationToken);
}

public interface ICommandHandler<TCommand>
    where TCommand : class
{
    void ConsumeEvent(
        StreamEvent evt,
        TCommand command,
        IStateContext state);

    ValueTask ExecuteAsync(
        ICommandContext<TCommand> context,
        CancellationToken cancellationToken);
}