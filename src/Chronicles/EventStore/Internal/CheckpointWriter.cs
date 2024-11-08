using Chronicles.Documents;

namespace Chronicles.EventStore.Internal;

internal class CheckpointWriter(
    TimeProvider dateTimeProvider,
    IDocumentWriter<CheckpointDocument<object?>> writer)
    : ICheckpointWriter
{
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