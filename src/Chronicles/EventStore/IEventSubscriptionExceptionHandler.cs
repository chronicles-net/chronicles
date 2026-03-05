namespace Chronicles.EventStore;

/// <summary>
/// Defines a handler for exceptions that occur during event subscription processing in Chronicles.
/// Implement this interface to provide custom logic for logging, or handling errors that arise when processing events from the event store.
/// Use when you need to control error handling behavior for event subscriptions, such as for diagnostics or resilience.
/// </summary>
public interface IEventSubscriptionExceptionHandler
{
    /// <summary>
    /// Handles an exception that occurred during event subscription processing.
    /// Implement this method to log, retry, or otherwise respond to errors in event subscription workflows.
    /// </summary>
    /// <param name="exception">The exception that occurred.</param>
    /// <param name="streamEvent">The stream event being processed when the exception occurred, if available.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> representing request cancellation.</param>
    /// <returns>A <see cref="ValueTask"/> representing the asynchronous operation.</returns>
    ValueTask HandleAsync(Exception exception, StreamEvent? streamEvent, CancellationToken cancellationToken);
}
