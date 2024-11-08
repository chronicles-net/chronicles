using Chronicles.Documents;

namespace Chronicles.EventStore.Internal;

internal record CheckpointDocument<TState>(
    string Id,
    string Pk,
    string Name,
    StreamId StreamId,
    StreamVersion StreamVersion,
    DateTimeOffset Timestamp,
    TState State)
    : Checkpoint<TState>(Name, StreamId, StreamVersion, Timestamp, State), IDocument
{
    string IDocument.GetDocumentId() => Id;

    string IDocument.GetPartitionKey() => Pk;
}
