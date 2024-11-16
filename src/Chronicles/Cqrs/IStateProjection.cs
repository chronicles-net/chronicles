using Chronicles.EventStore;

namespace Chronicles.Cqrs;

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
    /// <returns>State after applying event.</returns>
    TState ConsumeEvent(
        StreamEvent evt,
        TState state);
}
