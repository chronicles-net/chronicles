namespace Chronicles.EventStore.Internal;

internal sealed class DefaultEventSubscriptionExceptionHandler
    : IEventSubscriptionExceptionHandler
{
    public ValueTask HandleAsync(
        Exception exception,
        StreamEvent? streamEvent,
        CancellationToken cancellationToken)
        => ValueTask.CompletedTask;
}