namespace Chronicles.EventStore.Internal.EventConsumers;

internal interface IEventConsumer
{
    bool CanConsumeEvent(
        StreamEvent evt);

    ValueTask ConsumeAsync(
        StreamEvent evt,
        EventConsumerStateContext context,
        CancellationToken cancellationToken);

    ValueTask ConsumeAsync(
        StreamId streamId,
        StreamEvent[] events,
        EventConsumerStateContext context,
        CancellationToken cancellationToken);
}
