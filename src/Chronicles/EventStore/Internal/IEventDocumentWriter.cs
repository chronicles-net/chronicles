using System.Collections.Immutable;

namespace Chronicles.EventStore.Internal;

internal interface IEventDocumentWriter
{
    Task<StreamWriteResult> WriteStreamAsync(
        StreamMetadata metadata,
        IImmutableList<object> events,
        StreamWriteOptions? options = default,
        string? storeName = null,
        CancellationToken cancellationToken = default);
}
