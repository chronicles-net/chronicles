namespace Chronicles.EventStore.Internal;

internal interface ICheckpointReader
{
    Task<Checkpoint<T>?> ReadAsync<T>(
        string name,
        StreamId streamId,
        string? storeName,
        CancellationToken cancellationToken)
        where T : class;
}