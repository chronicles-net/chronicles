using Chronicles.Documents;
using Chronicles.EventStore;

namespace Chronicles.Cqrs;

public interface IDocumentProjectionRebuilder<TProjection, TDocument>
    where TDocument : class, IDocument
    where TProjection : class, IDocumentProjection<TDocument>
{
    Task<TDocument> RebuildAsync(
        StreamId streamId,
        CancellationToken cancellationToken);
}
