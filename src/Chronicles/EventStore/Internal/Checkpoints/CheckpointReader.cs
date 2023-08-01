using Chronicles.Cosmos;

namespace Chronicles.EventStore.Internal.Checkpoints;

internal class CheckpointReader
{
    private readonly ICosmosReader<Checkpoint> reader;

    public CheckpointReader(
        ICosmosReader<Checkpoint> reader)
        => this.reader = reader;

    public async Task<Checkpoint<T>?> ReadAsync<T>(
        string name,
        StreamId streamId,
        CancellationToken cancellationToken)
        where T : class
        => await reader
            .FindAsync<CheckpointDocument<T>>(
                name,
                streamId.Value,
                options: null,
                cancellationToken)
            .ConfigureAwait(false);
}