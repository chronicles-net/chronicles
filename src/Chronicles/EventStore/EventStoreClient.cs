using Chronicles.EventStore.Internal.Checkpoints;
using Chronicles.EventStore.Internal.Streams;

namespace Chronicles.EventStore;

internal class EventStoreClient : IEventStoreClient
{
    private readonly StreamEventReader streamReader;
    private readonly StreamEventWriter streamWriter;
    private readonly CheckpointReader checkpointReader;
    private readonly CheckpointWriter checkpointWriter;
    private readonly StreamMetadataReader metadataReader;

    public EventStoreClient(
        StreamEventReader streamReader,
        StreamEventWriter streamWriter,
        CheckpointReader checkpointReader,
        CheckpointWriter checkpointWriter,
        StreamMetadataReader metadataReader)
    {
        this.streamReader = streamReader;
        this.streamWriter = streamWriter;
        this.checkpointReader = checkpointReader;
        this.checkpointWriter = checkpointWriter;
        this.metadataReader = metadataReader;
    }

    public IAsyncEnumerable<StreamEvent> ReadStreamAsync(
        StreamId streamId,
        StreamVersion? fromVersion = null,
        StreamReadFilter? filter = null,
        CancellationToken cancellationToken = default)
        => streamReader
            .ReadAsync(
                streamId,
                Arguments.EnsureValueRange(fromVersion ?? StreamVersion.Any, nameof(fromVersion)),
                filter,
                cancellationToken);

    public Task<StreamMetadata> WriteStreamAsync(
        StreamId streamId,
        IReadOnlyCollection<object> events,
        StreamVersion? version = null,
        StreamWriteOptions? options = null,
        CancellationToken cancellationToken = default)
        => streamWriter
            .WriteAsync(
                streamId,
                Arguments.EnsureNoNullValues(events, nameof(events)),
                version ?? StreamVersion.Any,
                options,
                cancellationToken);

    public async Task<StreamMetadata> GetStreamMetadataAsync(
        StreamId streamId,
        CancellationToken cancellationToken = default)
        => await metadataReader
            .GetAsync(streamId, cancellationToken)
            .ConfigureAwait(false);

    public Task DeleteSubscriptionAsync(
        ConsumerGroup consumerGroup,
        CancellationToken cancellationToken = default)
        => throw new NotImplementedException();

    public Task<Checkpoint<T>?> GetStreamCheckpointAsync<T>(
        string name,
        StreamId streamId,
        CancellationToken cancellationToken = default)
        where T : class
        => checkpointReader
            .ReadAsync<T>(
                Arguments.EnsureNotNull(name, nameof(name)),
                streamId,
                cancellationToken);

    public IAsyncEnumerable<StreamMetadata> QueryStreamsAsync(
        string? filter = null,
        DateTimeOffset? createdAfter = null,
        CancellationToken cancellationToken = default)
        => throw new NotImplementedException();

    public Task SetStreamCheckpointAsync(
        string name,
        StreamId streamId,
        StreamVersion version,
        object? state = null,
        CancellationToken cancellationToken = default)
        => checkpointWriter
            .WriteAsync(
                Arguments.EnsureNotNull(name, nameof(name)),
                streamId,
                version,
                state,
                cancellationToken);

    public IStreamSubscription SubscribeToStreams(
        ConsumerGroup consumerGroup,
        SubscriptionStartOptions startOptions,
        ProcessEventsHandler eventsHandler,
        ProcessExceptionHandler exceptionHandler)
        => throw new NotImplementedException();
}
