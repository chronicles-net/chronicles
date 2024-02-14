namespace Chronicles.EventStore.Internal.EventConsumers;

public class EventConsumer<TConsumer>(
    TConsumer consumer,
    EventConsumerReflector<TConsumer> reflector) : IEventConsumer
    where TConsumer : class
{
    public bool CanConsumeEvent(
        StreamEvent evt)
        => reflector.CanConsumeEvent(evt);

    public ValueTask ConsumeAsync(
        StreamEvent evt,
        CancellationToken cancellationToken)
        => reflector.ConsumeAsync(evt, consumer, cancellationToken);

    public ValueTask ConsumeAsync(
        StreamId streamId,
        StreamEvent[] events,
        CancellationToken cancellationToken)
        => reflector.ConsumeAsync(streamId, events, consumer, cancellationToken);
}