namespace Chronicles.EventStore;

public interface IEventSubscriptionExceptionHandler
{
    ValueTask HandleAsync(
        Exception exception);
}