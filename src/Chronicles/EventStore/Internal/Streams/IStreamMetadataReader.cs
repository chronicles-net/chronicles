namespace Chronicles.EventStore.Internal.Streams;

internal interface IStreamMetadataReader
{
    Task<StreamMetadataDocument> GetAsync(
        StreamId streamId,
        string? storeName,
        CancellationToken cancellationToken);

    IAsyncEnumerable<StreamMetadataDocument> QueryAsync(
        string? filter,
        DateTimeOffset? createdAfter,
        string? storeName,
        CancellationToken cancellationToken);
}