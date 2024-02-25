using Chronicles.Documents;
using Chronicles.EventStore.Internal.EventConsumers;

namespace Chronicles.EventStore.Internal.Processors;

public class DocumentProjection<TState, TConsumer>(
    TConsumer consumer,
    EventConsumerReflector<TConsumer> reflector,
    IDocumentReader<TState> reader,
    IDocumentWriter<TState> writer)
    : IConsumeGroupedEventsAsync
    where TState : class, IDocument
    where TConsumer : class, IDocumentProjection<TState>
{
    public async Task ConsumeAsync(
        StreamId streamId,
        StreamEvent[] events,
        CancellationToken cancellationToken)
    {
        await consumer
            .ResumeAsync(ProjectionKind.Append, reader, streamId, events, cancellationToken)
            .ConfigureAwait(false);

        await reflector
            .ConsumeAsync(streamId, events, consumer, cancellationToken)
            .ConfigureAwait(false);

        await consumer
            .CommitAsync(ProjectionKind.Append, writer, cancellationToken)
            .ConfigureAwait(false);
    }
}