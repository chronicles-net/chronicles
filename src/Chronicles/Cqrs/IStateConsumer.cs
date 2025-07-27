using Chronicles.EventStore;

namespace Chronicles.Cqrs;

/// <summary>
/// Defines a consumer that processes events and updates or reacts to the current state.
/// State consumers are typically used for side effects, notifications, or further processing that depends on the state and incoming events such as read model projection.
/// Use this interface when you need to asynchronously handle events and state, such as updating external systems, sending messages, or triggering workflows.
/// </summary>
public interface IStateConsumer<TState>
{
    /// <summary>
    /// Asynchronously consumes an event and the current state, performing any required processing or side effects.
    /// </summary>
    /// <param name="evt">The event to consume.</param>
    /// <param name="state">The current state, which may be null if not initialized.</param>
    /// <param name="cancellationToken">Token to observe for cancellation.</param>
    /// <returns>A ValueTask representing the asynchronous operation.</returns>
    ValueTask ConsumeAsync(
        StreamEvent evt,
        TState? state,
        CancellationToken cancellationToken);
}
