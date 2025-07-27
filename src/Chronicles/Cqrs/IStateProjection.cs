using Chronicles.EventStore;

namespace Chronicles.Cqrs;

/// <summary>
/// Defines a projection that builds and maintains a state object by consuming events from an event stream.
/// State projections are used to derive and update read models or aggregate state from a sequence of events.
/// Use this interface when you need to reconstruct or update state based on event sourcing,
/// typically for queries or business logic that depends on the current state.
/// </summary>
public interface IStateProjection<TState>
{
    /// <summary>
    /// Initialize state object for projection.
    /// </summary>
    /// <param name="streamId">StreamId state is based on.</param>
    /// <returns>State object.</returns>
    TState CreateState(
        StreamId streamId);

    /// <summary>
    /// Consume event and update state.
    /// </summary>
    /// <param name="evt">Event to consume.</param>
    /// <param name="state">State to update.</param>
    /// <returns>State after applying event or <c>null</c> if state has not changed.</returns>
    TState? ConsumeEvent(
        StreamEvent evt,
        TState state);
}
