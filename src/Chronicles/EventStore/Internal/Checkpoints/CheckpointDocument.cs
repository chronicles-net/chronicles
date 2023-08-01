using Chronicles.Cosmos;

namespace Chronicles.EventStore.Internal.Checkpoints;

internal record CheckpointDocument<TState>(
    string Id,
    string Pk,
    string Name,
    StreamId StreamId,
    StreamVersion StreamVersion,
    DateTimeOffset Timestamp,
    TState State)
    : Checkpoint<TState>(Name, StreamId, StreamVersion, Timestamp, State), ICosmosDocument
{
    string ICosmosDocument.GetDocumentId()
        => Name;

    string ICosmosDocument.GetPartitionKey()
        => StreamId.Value;
}
