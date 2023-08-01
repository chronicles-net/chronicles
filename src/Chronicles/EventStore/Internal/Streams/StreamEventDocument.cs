using System.Text.Json.Serialization;

namespace Chronicles.EventStore.Internal.Streams;

/// <summary>
/// Represents an event as a cosmos document written to a stream.
/// </summary>
/// <param name="Id">Version/position within the stream</param>
/// <param name="Pk">Partition key aka stream id</param>
/// <param name="Properties">Meta data on the event</param>
/// <param name="Data">Event object</param>
internal record StreamEventDocument(
    [property: JsonPropertyName(JsonPropertyNames.Id)] string Id,
    [property: JsonPropertyName(JsonPropertyNames.PartitionKey)] string Pk,
    [property: JsonPropertyName(JsonPropertyNames.Properties)] EventMetadata Properties,
    [property: JsonPropertyName(JsonPropertyNames.Data)] object Data)
    : StreamDocument()
{
    protected override string GetDocumentId() => Id;

    protected override string GetPartitionKey() => Pk;
}