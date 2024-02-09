namespace Chronicles.EventStore.Internal.EventConsumers;

internal interface IEventConsumerReflector
{
    bool CanConsumeEvent(
        StreamEvent evt);

    bool CanConsumeOneOrMoreEvents(
        IEnumerable<StreamEvent> events);

    ValueTask ConsumeAsync(
        StreamEvent evt,
        object projection,
        CancellationToken cancellationToken);

    bool IsNotConsumingEvents();
}