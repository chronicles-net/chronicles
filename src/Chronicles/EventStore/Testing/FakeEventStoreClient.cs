namespace Chronicles.EventStore.Testing;

public sealed class FakeEventStoreClient : IEventStoreClient
{
    Task<Checkpoint<T>?> IEventStoreClient.GetStreamCheckpointAsync<T>(
        string name,
        StreamId streamId,
        string? storeName,
        CancellationToken cancellationToken)
        where T : class
        => throw new NotImplementedException();

    Task<StreamMetadata> IEventStoreClient.GetStreamMetadataAsync(
        StreamId streamId,
        string? storeName,
        CancellationToken cancellationToken)
        => throw new NotImplementedException();

    IAsyncEnumerable<StreamMetadata> IEventStoreClient.QueryStreamsAsync(
        string? filter,
        DateTimeOffset? createdAfter,
        string? storeName,
        CancellationToken cancellationToken)
        => throw new NotImplementedException();

    IAsyncEnumerable<StreamEvent> IEventStoreClient.ReadStreamAsync(
        StreamId streamId,
        StreamVersion? fromVersion,
        StreamReadFilter? filter,
        string? storeName,
        CancellationToken cancellationToken)
        => throw new NotImplementedException();

    Task IEventStoreClient.SetStreamCheckpointAsync(
        string name,
        StreamId streamId,
        StreamVersion version,
        object? state,
        string? storeName,
        CancellationToken cancellationToken)
        => throw new NotImplementedException();

    Task<StreamMetadata> IEventStoreClient.WriteStreamAsync(
        StreamId streamId,
        IReadOnlyCollection<object> events,
        StreamVersion? version,
        StreamWriteOptions? options,
        string? storeName,
        CancellationToken cancellationToken)
        => throw new NotImplementedException();
}