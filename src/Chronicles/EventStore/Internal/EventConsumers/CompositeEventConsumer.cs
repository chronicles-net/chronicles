namespace Chronicles.EventStore.Internal.EventConsumers;

internal class CompositeEventConsumer(
    IEnumerable<IEventConsumer> consumers) : IEventConsumer
{
    public bool CanConsumeEvent(
        StreamEvent evt)
        => consumers.Any(c => c.CanConsumeEvent(evt));

    public async ValueTask ConsumeAsync(
        StreamEvent evt,
        EventConsumerStateContext context,
        CancellationToken cancellationToken)
    {
        foreach (var consumer in consumers)
        {
            await consumer.ConsumeAsync(
                evt,
                new EventConsumerStateContext(consumer),
                cancellationToken);
        }
    }

    public async ValueTask ConsumeAsync(
        StreamId streamId,
        StreamEvent[] events,
        EventConsumerStateContext context,
        CancellationToken cancellationToken)
    {
        foreach (var consumer in consumers)
        {
            await consumer.ConsumeAsync(
                streamId,
                events,
                new EventConsumerStateContext(consumer),
                cancellationToken);
        }
    }
}
