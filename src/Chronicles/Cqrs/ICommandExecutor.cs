using Chronicles.EventStore;

namespace Chronicles.Cqrs;

/// <summary>
/// Defines a command executor for CQRS that orchestrates the execution of a command against a stream of events.
/// A command executor is responsible for replaying events to reconstruct state, then executing the command using the provided context.
/// </summary>
/// <remarks>
/// Use this interface when you need a customized command execution.
/// </remarks>
/// <typeparam name="TCommand">The type of the command to execute.</typeparam>
public interface ICommandExecutor<TCommand>
    where TCommand : class
{
    /// <summary>
    /// Asynchronously executes the command using the provided event stream and command context.
    /// Replays events to reconstruct state, then invokes the command handler to process the command and produce resulting events.
    /// </summary>
    /// <param name="events">The stream of events to replay for state reconstruction.</param>
    /// <param name="context">The context for the command execution, providing access to the command, metadata, and state.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A <see cref="ValueTask"/> representing the asynchronous operation.</returns>
    ValueTask ExecuteAsync(
        IAsyncEnumerable<StreamEvent> events,
        ICommandContext<TCommand> context,
        CancellationToken cancellationToken);
}
