using System.Runtime.CompilerServices;
using Chronicles.EventStore.Internal.Checkpoints;
using Chronicles.EventStore.Internal.Streams;

namespace Chronicles.EventStore;

internal class EventStoreClient(
    IStreamEventReader streamReader,
    IStreamEventWriter streamWriter,
    CheckpointReader checkpointReader,
    CheckpointWriter checkpointWriter,
    IStreamMetadataReader metadataReader)
    : IEventStoreClient
{
    public async IAsyncEnumerable<StreamEvent> ReadStreamAsync(
        StreamId streamId,
        StreamReadOptions? options = null,
        string? storeName = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var metadata = await metadataReader
            .GetAsync(streamId, storeName, cancellationToken)
            .ConfigureAwait(false);

        var events = streamReader
            .ReadAsync(
                streamId,
                options,
                metadata,
                storeName: storeName,
                cancellationToken);
        await foreach (var evt in events)
        {
            yield return evt;
        }
    }

    public Task<StreamMetadata> WriteStreamAsync(
        StreamId streamId,
        IReadOnlyCollection<object> events,
        StreamWriteOptions? options = null,
        string? storeName = null,
        CancellationToken cancellationToken = default)
        => streamWriter
            .WriteAsync(
                streamId,
                Arguments.EnsureNoNullValues(events, nameof(events)),
                options,
                storeName: storeName,
                cancellationToken);

    public async Task<StreamMetadata> GetStreamMetadataAsync(
        StreamId streamId,
        string? storeName = null,
        CancellationToken cancellationToken = default)
        => await metadataReader
            .GetAsync(
                streamId,
                storeName: storeName,
                cancellationToken);

    public Task<Checkpoint<T>?> GetStreamCheckpointAsync<T>(
        string name,
        StreamId streamId,
        string? storeName = null,
        CancellationToken cancellationToken = default)
        where T : class
        => checkpointReader
            .ReadAsync<T>(
                Arguments.EnsureNotNull(name, nameof(name)),
                streamId,
                storeName: storeName,
                cancellationToken);

    public IAsyncEnumerable<StreamMetadata> QueryStreamsAsync(
        string? filter = null,
        DateTimeOffset? createdAfter = null,
        string? storeName = null,
        CancellationToken cancellationToken = default)
        => metadataReader
            .QueryAsync(
                filter,
                createdAfter,
                storeName: storeName,
                cancellationToken);

    public Task SetStreamCheckpointAsync(
        string name,
        StreamId streamId,
        StreamVersion version,
        object? state = null,
        string? storeName = null,
        CancellationToken cancellationToken = default)
        => checkpointWriter
            .WriteAsync(
                Arguments.EnsureNotNull(name, nameof(name)),
                streamId,
                version,
                state,
                storeName: storeName,
                cancellationToken);
}