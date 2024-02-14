namespace Chronicles.EventStore.Internal.EventConsumers;

internal interface IEventConsumer
{
    bool CanConsumeEvent(
        StreamEvent evt);

    ValueTask ConsumeAsync(
        StreamEvent evt,
        CancellationToken cancellationToken);

    ValueTask ConsumeAsync(
        StreamId streamId,
        StreamEvent[] events,
        CancellationToken cancellationToken);
}
