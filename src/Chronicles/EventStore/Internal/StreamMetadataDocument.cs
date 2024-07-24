using System.Text.Json.Serialization;

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
        Timestamp)
{
    protected override string GetDocumentId() => Id;

    protected override string GetPartitionKey() => Pk;

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