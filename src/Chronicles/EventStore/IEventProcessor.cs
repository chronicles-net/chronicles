namespace Chronicles.EventStore;

/// <summary>
/// Implementing this interface will instruct the framework to
/// call <see cref="ConsumeAsync(StreamEvent, IStateContext, bool, CancellationToken)"/> for every
/// event in the event stream.
/// </summary>
public interface IEventProcessor
{
    /// <summary>
    /// Called for every event in the stream.
    /// </summary>
    /// <param name="evt">The event read from stream.</param>
    /// <param name="state">Context to set or get state.</param>
    /// <param name="hasMore"><c>True</c> if more events are forth coming</param>
    /// <param name="cancellationToken">(Optional) <seealso cref="CancellationToken"/> representing request cancellation.</param>
    /// <returns>A <see cref="ValueTask"/> representing the asynchronous operation.</returns>
    ValueTask ConsumeAsync(
        StreamEvent evt,
        IStateContext state,
        bool hasMore,
        CancellationToken cancellationToken);
}