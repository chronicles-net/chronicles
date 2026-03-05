using Chronicles.Cqrs.Internal.EventProcessors;
using Chronicles.Documents;
using Chronicles.EventStore;

namespace Chronicles.Cqrs.Internal;

internal class DocumentProjectionRebuilder<TProjection, TDocument>(
    string? storeName,
    TProjection projection,
    DocumentProjectionProcessor<TProjection, TDocument> processor,
    IEventStreamReader reader)
    : IDocumentProjectionRebuilder<TProjection, TDocument>
    where TDocument : class, IDocument
    where TProjection : class, IDocumentProjection<TDocument>
{
    public async Task<TDocument> RebuildAsync(
        StreamId streamId,
        CancellationToken cancellationToken)
    {
        var initialState = projection.CreateState(streamId);
        var stateContext = IStateContext.Create();
        stateContext.SetState(initialState);

        StreamEvent? previous = null;
        await foreach (var evt in reader.ReadAsync(streamId, storeName: storeName, cancellationToken: cancellationToken))
        {
            if (previous != null)
            {
                await processor.ConsumeAsync(previous, stateContext, true, cancellationToken);
            }

            previous = evt;
        }

        if (previous != null)
        {
            await processor.ConsumeAsync(previous, stateContext, false, cancellationToken);
        }

        return stateContext.GetState<TDocument>() ?? initialState;
    }
}
