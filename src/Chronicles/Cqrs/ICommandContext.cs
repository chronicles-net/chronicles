using Chronicles.EventStore;

namespace Chronicles.Cqrs;

public interface ICommandContext<TCommand>
    where TCommand : class
{
    /// <summary>
    /// Gets the command being executed.
    /// </summary>
    TCommand Command { get; }

    /// <summary>
    /// Gets the current state of the stream.
    /// </summary>
    StreamMetadata Metadata { get; }

    /// <summary>
    /// Gets or sets the response of the command.
    /// </summary>
    object? Response { get; set; }

    /// <summary>
    /// Adds an event to stream.
    /// </summary>
    /// <remarks>
    /// Events are first committed to the stream after <see cref="ICommandHandler{TCommand}.ExecuteAsync"/> completes.
    /// </remarks>
    /// <param name="evt">Event to add</param>
    /// <returns>returns self.</returns>
    ICommandContext<TCommand> AddEvent(object evt);

    /// <summary>
    /// Triggered after the events added by <see cref="AddEvent(object)"/> have been committed to the stream.
    /// </summary>
#pragma warning disable CA1003 // Use generic event handler instances
    event CommandCompletedAsync<TCommand>? Completed;
#pragma warning restore CA1003 // Use generic event handler instances

    IStateContext State { get; }
}

public interface ICommandContext<TCommand, TState>
    : ICommandContext<TCommand>
    where TCommand : class
    where TState : class
{
    TState CurrentState { get; }
}