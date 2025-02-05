using Chronicles.Documents;
using Chronicles.EventStore;

namespace Chronicles.Cqrs.Internal.EventProcessors;

internal class DocumentProjectionProcessor<TConsumer, TDocument>(
    TConsumer consumer,
    IDocumentReader<TDocument> reader,
    IDocumentWriter<TDocument> writer)
    : StateProjectionProcessor<TConsumer, TDocument>(consumer)
    where TDocument : class, IDocument
    where TConsumer : class, IDocumentProjection<TDocument>
{
    private readonly IDocumentReader<TDocument> reader = reader;
    private readonly IDocumentWriter<TDocument> writer = writer;

    protected override async Task<TDocument> GetStateAsync(
        StreamEvent evt,
        IStateContext state,
        CancellationToken cancellationToken)
    {
        var document = state.GetState<TDocument>();
        if (document is null)
        {
            document = await base
                .GetStateAsync(evt, state, cancellationToken)
                .ConfigureAwait(false);
            document = await reader
                .FindAsync(
                    document.GetDocumentId(),
                    document.GetPartitionKey(),
                    cancellationToken)
                .ConfigureAwait(false)
             ?? document;

            state.SetState(document);
        }

        return document;
    }

    protected override async Task CommitAsync(
        TDocument? document,
        IStateContext state,
        CancellationToken cancellationToken)
    {
        if (document != null)
        {
            switch (await consumer.OnCommitAsync(document, cancellationToken))
            {
                case DocumentCommitAction.Update:
                    await writer
                        .WriteAsync(document, cancellationToken)
                        .ConfigureAwait(false);
                    break;

                case DocumentCommitAction.Delete:
                    await writer
                        .DeleteAsync(
                            document.GetDocumentId(),
                            document.GetPartitionKey(),
                            cancellationToken)
                        .ConfigureAwait(false);
                    break;
            }
        }
    }
}
