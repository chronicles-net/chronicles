using Chronicles.Documents;
using Chronicles.EventStore;

namespace Chronicles.Cqrs;

/// <summary>
/// Defines a service for rebuilding a document projection from an event stream.
/// Rebuilding a projection means reconstructing the document's state by replaying all relevant events from the stream.
/// Use this interface when you need to restore, refresh, or recover the read model or aggregate state for a document,
/// such as after schema changes, data corruption, or to ensure consistency.
/// </summary>
/// <typeparam name="TProjection">The type of the document projection to rebuild.</typeparam>
/// <typeparam name="TDocument">The type of the document being projected.</typeparam>
public interface IDocumentProjectionRebuilder<TProjection, TDocument>
    where TDocument : class, IDocument
    where TProjection : class, IDocumentProjection<TDocument>
{
    /// <summary>
    /// Asynchronously rebuilds the document projection for the specified stream by replaying all events.
    /// </summary>
    /// <param name="streamId">The identifier of the event stream to rebuild the projection from.</param>
    /// <param name="cancellationToken">Token to observe for cancellation.</param>
    /// <returns>The rebuilt document projection.</returns>
    Task<TDocument> RebuildAsync(
        StreamId streamId,
        CancellationToken cancellationToken);
}
