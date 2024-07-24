using System.Runtime.CompilerServices;

namespace Chronicles.EventStore.Internal;

internal class EventStreamReader(
    ICheckpointReader checkpointReader,
    IStreamMetadataReader metadataReader,
    IEventDocumentReader eventDocumentReader)
    : IEventStreamReader
{
    public async Task<Checkpoint<TState>?> GetCheckpointAsync<TState>(
        string name,
        StreamId streamId,
        string? storeName = null,
        CancellationToken cancellationToken = default)
        where TState : class
        => await checkpointReader
            .ReadAsync<TState>(
                name,
                streamId,
                storeName,
                cancellationToken)
            .ConfigureAwait(false);

    public async Task<StreamMetadata> GetMetadataAsync(
        StreamId streamId,
        string? storeName = null,
        CancellationToken cancellationToken = default)
        => await metadataReader
            .GetAsync(
                streamId,
                storeName: storeName,
                cancellationToken)
            .ConfigureAwait(false);

    public IAsyncEnumerable<StreamMetadata> QueryStreamsAsync(
        string? filter = null,
        DateTimeOffset? createdAfter = null,
        string? storeName = null,
        CancellationToken cancellationToken = default)
        => metadataReader
            .QueryAsync(
                filter,
                createdAfter,
                storeName,
                cancellationToken);

    public async IAsyncEnumerable<StreamEvent> ReadAsync(
        StreamId streamId,
        StreamReadOptions? options = null,
        string? storeName = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var metadata = options?.Metadata
                    ?? await metadataReader
                            .GetAsync(streamId, storeName, cancellationToken)
                            .ConfigureAwait(false);

        metadata.EnsureSuccess(options);

        var events = eventDocumentReader
            .ReadAsync(
                metadata,
                options,
                storeName: storeName,
                cancellationToken);
        await foreach (var evt in events)
        {
            yield return evt;
        }
    }
}
