namespace Chronicles.EventStore.Internal;

internal interface IEventDocumentReader
{
    IAsyncEnumerable<StreamEvent> ReadAsync(
        StreamMetadata metadata,
        StreamReadOptions? options,
        string? storeName,
        CancellationToken cancellationToken);
}