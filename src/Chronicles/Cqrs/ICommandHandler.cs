using Chronicles.EventStore;

namespace Chronicles.Cqrs;

/// <summary>
/// Defines a handler for CQRS commands that operates on a specific state type and supports state projection.
/// A CQRS command handler processes commands that intend to change application state, typically by validating input, applying business logic, and producing events.
/// Use this interface when implementing the write side of a CQRS architecture, where commands are used to request changes and state is managed via event sourcing or projections.
/// </summary>
/// <typeparam name="TCommand">The type of the command to handle.</typeparam>
/// <typeparam name="TState">The type of the state associated with the command.</typeparam>
public interface ICommandHandler<TCommand, TState>
    : IStateProjection<TState>
    where TCommand : class
    where TState : class
{
    /// <summary>
    /// Executes the command asynchronously using the provided context and state.
    /// Validates and processes the command, applies business logic, and adds resulting events to the context for persistence.
    /// </summary>
    /// <remarks>
    /// The <paramref name="context"/> provides access to the command, its metadata, and allows you to add events that will be persisted to the stream after command execution completes.
    /// </remarks>
    /// <param name="context">The context for the command execution.</param>
    /// <param name="state">The current state associated with the command.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A <see cref="ValueTask"/> representing the asynchronous operation.</returns>
    ValueTask ExecuteAsync(
        ICommandContext<TCommand> context,
        TState state,
        CancellationToken cancellationToken);
}

/// <summary>
/// Defines a handler for CQRS commands that does not require a specific state type.
/// Use this interface for commands that operate without a dedicated state or when state is managed externally.
/// </summary>
/// <remarks>
/// This type of command handler is much more performance-oriented and does not require a state projection e.i. no need to read all events from the stream.
/// </remarks>
/// <typeparam name="TCommand">The type of the command to handle.</typeparam>
public interface ICommandHandler<TCommand>
    where TCommand : class
{
    /// <summary>
    /// Consumes an event and updates the state context for the given command.
    /// Used to apply events to the state context, typically for event sourcing or updating projections.
    /// </summary>
    /// <param name="evt">The event to consume.</param>
    /// <param name="command">The command being handled.</param>
    /// <param name="state">The state context to update.</param>
    void ConsumeEvent(
        StreamEvent evt,
        TCommand command,
        IStateContext state);

    /// <summary>
    /// Executes the command asynchronously using the provided context.
    /// Validates and processes the command, applies business logic, and adds resulting events to the context for persistence.
    /// </summary>
    /// <remarks>
    /// The <paramref name="context"/> provides access to the command, its metadata, and allows you to add events that will be persisted to the stream after command execution completes.
    /// </remarks>
    /// <param name="context">The context for the command execution.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A <see cref="ValueTask"/> representing the asynchronous operation.</returns>
    ValueTask ExecuteAsync(
        ICommandContext<TCommand> context,
        CancellationToken cancellationToken);
}
