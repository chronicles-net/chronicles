using Chronicles.Documents;

namespace Chronicles.EventStore.Internal.Checkpoints;

internal class CheckpointWriter
{
    private readonly IDateTimeProvider dateTimeProvider;
    private readonly IDocumentWriter<CheckpointDocument<object?>> writer;

    public CheckpointWriter(
        IDateTimeProvider dateTimeProvider,
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
                    dateTimeProvider.GetDateTime(),
                    state),
                options: null,
                storeName: storeName,
                cancellationToken: cancellationToken)
            .ConfigureAwait(false);
}