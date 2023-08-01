using System.Text.Json.Serialization;

namespace Chronicles.EventStore.Internal.Streams;

internal sealed record StreamMetadataDocument(
    [property: JsonPropertyName(JsonPropertyNames.Id)] string Id,
    [property: JsonPropertyName(JsonPropertyNames.PartitionKey)] string Pk,
    StreamId StreamId,
    StreamState State,
    StreamVersion Version,
    DateTimeOffset Timestamp,
    [property: JsonPropertyName(JsonPropertyNames.Etag)] string? Etag)
    : StreamMetadata(
        StreamId,
        State,
        Version,
        Timestamp)
{
    protected override string GetDocumentId() => Id;

    protected override string GetPartitionKey() => Pk;
}