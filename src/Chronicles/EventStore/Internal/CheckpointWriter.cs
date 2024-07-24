using Chronicles.Documents;

namespace Chronicles.EventStore.Internal;

internal class CheckpointWriter : ICheckpointWriter
{
    private readonly TimeProvider dateTimeProvider;
    private readonly IDocumentWriter<CheckpointDocument<object?>> writer;

    public CheckpointWriter(
        TimeProvider dateTimeProvider,
        IDocumentWriter<CheckpointDocument<object?>> writer)
    {
        this.dateTimeProvider = dateTimeProvider;
        this.writer = writer;
    }

    public async Task WriteAsync(
        string name,
        StreamId streamId,
        StreamVersion version,
        object? state,
        string? storeName,
        CancellationToken cancellationToken)
        => await writer
            .WriteAsync(
                new CheckpointDocument<object?>(
                    name,
                    (string)streamId,
                    name,
                    streamId,
                    version,
                    dateTimeProvider.GetUtcNow(),
                    state),
                options: null,
                storeName: storeName,
                cancellationToken: cancellationToken)
            .ConfigureAwait(false);
}