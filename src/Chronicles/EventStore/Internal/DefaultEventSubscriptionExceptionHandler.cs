namespace Chronicles.EventStore.Internal;

internal sealed class DefaultEventSubscriptionExceptionHandler
    : IEventSubscriptionExceptionHandler
{
    public ValueTask HandleAsync(
        Exception exception)
        => ValueTask.CompletedTask;
}