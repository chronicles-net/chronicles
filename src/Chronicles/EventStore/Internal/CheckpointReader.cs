using Chronicles.Documents;

namespace Chronicles.EventStore.Internal;

internal class CheckpointReader : ICheckpointReader
{
    private readonly IDocumentReader<Checkpoint> reader;

    public CheckpointReader(
        IDocumentReader<Checkpoint> reader)
        => this.reader = reader;

    public async Task<Checkpoint<T>?> ReadAsync<T>(
        string name,
        StreamId streamId,
        string? storeName,
        CancellationToken cancellationToken)
        where T : class
        => await reader
            .FindAsync<Checkpoint, CheckpointDocument<T>>(
                name,
                (string)streamId,
                options: null,
                storeName: storeName,
                cancellationToken)
            .ConfigureAwait(false);
}