namespace Chronicles.EventStore.Internal.Streams;

public interface IStreamEventWriter
{
    Task<StreamMetadata> WriteAsync(
        StreamId streamId,
        IReadOnlyCollection<object> events,
        StreamWriteOptions? options,
        string? storeName,
        CancellationToken cancellationToken);
}