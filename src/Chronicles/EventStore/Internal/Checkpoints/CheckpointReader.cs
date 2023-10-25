using Chronicles.Documents;

namespace Chronicles.EventStore.Internal.Checkpoints;

internal class CheckpointReader
{
    private readonly IDocumentReader<Checkpoint> reader;

    public CheckpointReader(
        IDocumentReader<Checkpoint> reader)
        => this.reader = reader;

    public async Task<Checkpoint<T>?> ReadAsync<T>(
        string name,
        StreamId streamId,
        CancellationToken cancellationToken)
        where T : class
        => await reader
            .FindAsync<Checkpoint, CheckpointDocument<T>>(
                name,
                streamId.ToString(),
                options: null,
                storeName: null,
                cancellationToken)
            .ConfigureAwait(false);
}