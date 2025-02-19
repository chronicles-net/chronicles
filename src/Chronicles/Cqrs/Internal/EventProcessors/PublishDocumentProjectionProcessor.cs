using Chronicles.Documents;
using Chronicles.EventStore;

namespace Chronicles.Cqrs.Internal.EventProcessors;

internal class PublishDocumentProjectionProcessor<TConsumer, TDocument, TPublisher>(
    TConsumer consumer,
    IDocumentReader<TDocument> reader,
    IDocumentWriter<TDocument> writer,
    TPublisher publisher)
    : DocumentProjectionProcessor<TConsumer, TDocument>(consumer, reader, writer)
    where TDocument : class, IDocument
    where TConsumer : class, IDocumentProjection<TDocument>
    where TPublisher : class, IDocumentPublisher<TDocument>
{
    protected override async Task OnDocumentCommittedAsync(
        DocumentCommitAction commitAction,
        TDocument document,
        IStateContext state,
        CancellationToken cancellationToken)
    {
        if (commitAction == DocumentCommitAction.None)
        {
            return;
        }

        await publisher.PublishAsync(document, cancellationToken);
    }
}

