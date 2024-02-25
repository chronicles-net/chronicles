using Chronicles.Documents;
using Chronicles.EventStore.Internal.EventConsumers;
using Chronicles.EventStore.Internal.Streams;

namespace Chronicles.EventStore.Internal.Processors;

internal class EventDocumentProcessor
  : IDocumentProcessor<StreamEvent>
{
    private readonly string name;
    private readonly IEventConsumerFactory factory;
    private readonly EventSubscriptionOptions options;

    public EventDocumentProcessor(
        string name,
        IEventConsumerFactory factory,
        EventSubscriptionOptions options)
    {
        this.name = name;
        this.factory = factory;
        this.options = options;
    }

    public Task ErrorAsync(
      string leaseToken,
      Exception exception)
      => Task.CompletedTask;

    public async Task ProcessAsync(
      IReadOnlyCollection<StreamEvent> changes,
      CancellationToken cancellationToken)
    {
        var groups = changes
            .Where(e => e.Data is not StreamMetadataDocument)
            .Where(options.Filter)
            .GroupBy(e => e.Metadata.StreamId);

        foreach (var group in groups)
        {
            var consumer = factory.CreateConsumer(name);
            var context = new EventConsumerStateContext(consumer);
            await consumer
                .ConsumeAsync(group.Key, [.. group], context, cancellationToken)
                .ConfigureAwait(false);
        }
    }
}