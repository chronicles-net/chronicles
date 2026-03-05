using System.Text.Json.Serialization;
using Chronicles.Documents;

namespace Chronicles.EventStore.Internal;

internal sealed record StreamMetadataDocument(
    [property: JsonPropertyName(JsonPropertyNames.Id)] string Id,
    [property: JsonPropertyName(JsonPropertyNames.PartitionKey)] string Pk,
    StreamId StreamId,
    StreamState State,
    StreamVersion Version,
    DateTimeOffset Timestamp)
    : StreamMetadata(
        StreamId,
        State,
        Version,
        Timestamp),
    IDocument
{
    string IDocument.GetDocumentId() => Id;

    string IDocument.GetPartitionKey() => Pk;

    internal static StreamMetadataDocument FromMetadata(
        StreamMetadata metadata)
        => new(
            Id: JsonPropertyNames.StreamMetadataId,
            Pk: (string)metadata.StreamId,
            metadata.StreamId,
            metadata.State,
            metadata.Version,
            metadata.Timestamp);
}