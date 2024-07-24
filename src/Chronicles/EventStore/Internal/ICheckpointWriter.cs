namespace Chronicles.EventStore.Internal;

internal interface ICheckpointWriter
{
    Task WriteAsync(
        string name,
        StreamId streamId,
        StreamVersion version,
        object? state,
        string? storeName,
        CancellationToken cancellationToken);
}