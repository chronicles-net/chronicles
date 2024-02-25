namespace Chronicles.EventStore.Internal.EventConsumers;

public class EventConsumer<TConsumer>(
    TConsumer consumer,
    EventConsumerStateReflector<TConsumer> reflector) : IEventConsumer
    where TConsumer : class
{
    public bool CanConsumeEvent(
        StreamEvent evt)
        => reflector.CanConsumeEvent(evt);

    public ValueTask ConsumeAsync(
        StreamEvent evt,
        EventConsumerStateContext context,
        CancellationToken cancellationToken)
        => reflector.ConsumeAsync(
            evt,
            consumer,
            new EventConsumerStateContext(consumer),
            cancellationToken);

    public ValueTask ConsumeAsync(
        StreamId streamId,
        StreamEvent[] events,
        EventConsumerStateContext context,
        CancellationToken cancellationToken)
        => reflector.ConsumeAsync(
            streamId,
            events,
            consumer,
            new EventConsumerStateContext(consumer),
            cancellationToken);
}