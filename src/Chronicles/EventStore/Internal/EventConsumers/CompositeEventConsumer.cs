namespace Chronicles.EventStore.Internal.EventConsumers;

internal class CompositeEventConsumer(
    IEnumerable<IEventConsumer> consumers) : IEventConsumer
{
    public bool CanConsumeEvent(
        StreamEvent evt)
        => consumers.Any(c => c.CanConsumeEvent(evt));

    public async ValueTask ConsumeAsync(
        StreamEvent evt,
        CancellationToken cancellationToken)
    {
        foreach (var consumer in consumers)
        {
            await consumer.ConsumeAsync(evt, cancellationToken);
        }
    }

    public async ValueTask ConsumeAsync(
        StreamId streamId,
        StreamEvent[] events,
        CancellationToken cancellationToken)
    {
        foreach (var consumer in consumers)
        {
            await consumer.ConsumeAsync(streamId, events, cancellationToken);
        }
    }
}
