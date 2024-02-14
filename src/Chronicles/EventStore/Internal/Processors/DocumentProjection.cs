using Chronicles.Documents;
using Chronicles.EventStore.Internal.EventConsumers;

namespace Chronicles.EventStore.Internal.Processors;

public class DocumentProjection<TState, TConsumer>
    : IConsumeGroupedEventsAsync
    where TState : class, IDocument
    where TConsumer : class, IDocumentProjection<TState>
{
    private readonly TConsumer consumer;
    private readonly EventConsumerReflector<TConsumer> reflector;
    private readonly IDocumentReader<TState> reader;
    private readonly IDocumentWriter<TState> writer;

    public DocumentProjection(
        TConsumer consumer,
        EventConsumerReflector<TConsumer> reflector,
        IDocumentReader<TState> reader,
        IDocumentWriter<TState> writer)
    {
        this.consumer = consumer;
        this.reflector = reflector;
        this.reader = reader;
        this.writer = writer;
    }

    public async Task ConsumeAsync(
        StreamId streamId,
        StreamEvent[] events,
        CancellationToken cancellationToken)
    {
        await consumer
            .ResumeAsync(reader, streamId, events, cancellationToken)
            .ConfigureAwait(false);

        await reflector
            .ConsumeAsync(streamId, events, consumer, cancellationToken)
            .ConfigureAwait(false);

        await consumer
            .CommitAsync(writer, cancellationToken)
            .ConfigureAwait(false);
    }
}