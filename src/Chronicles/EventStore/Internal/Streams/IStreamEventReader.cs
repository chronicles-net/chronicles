namespace Chronicles.EventStore.Internal.Streams;

public interface IStreamEventReader
{
    IAsyncEnumerable<StreamEvent> ReadAsync(
        StreamId streamId,
        StreamReadOptions? options,
        StreamMetadata metadata,
        string? storeName,
        CancellationToken cancellationToken);
}