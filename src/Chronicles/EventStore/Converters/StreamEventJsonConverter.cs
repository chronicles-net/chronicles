using System.Text.Json;
using System.Text.Json.Serialization;
using Chronicles.EventStore.Events;

namespace Chronicles.EventStore.Converters;

/// <summary>
/// Responsible for converting an event envelope to and from json without loosing underlying event type.
/// </summary>
public sealed class StreamEventJsonConverter : JsonConverter<StreamEvent>
{
    private readonly IStreamEventConverter dataConverter;

    public StreamEventJsonConverter(
        IStreamEventConverter dataConverter)
        => this.dataConverter = dataConverter;

    public override StreamEvent Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.StartObject)
        {
            throw new JsonException();
        }

        using var jsonDocument = JsonDocument.ParseValue(ref reader);

        // If we are reading the meta-data document, then skip it.
        if (jsonDocument.RootElement.TryGetProperty(EventMetadataNames.Id, out var id)
            && id.GetString() == EventMetadataNames.StreamMetadataId)
        {
            return default!;
        }

        if (jsonDocument.RootElement.TryGetProperty(EventMetadataNames.Properties, out var properties))
        {
            try
            {
                var metadata = properties.Deserialize<EventMetadata>(options);
                if (metadata is not null && jsonDocument.RootElement.TryGetProperty(EventMetadataNames.Data, out var data))
                {
                    return dataConverter.Convert(
                        new(data, metadata, options));
                }
            }
            catch (Exception ex)
            {
                return new StreamEvent(
                    new FaultedEvent(
                        jsonDocument.RootElement.GetRawText(),
                        ex),
                    EventMetadata.Empty);
            }
        }

        return new StreamEvent(
            new FaultedEvent(
                jsonDocument.RootElement.GetRawText(),
                null),
            EventMetadata.Empty);
    }

    public override void Write(
        Utf8JsonWriter writer,
        StreamEvent value,
        JsonSerializerOptions options)
        => throw new NotImplementedException();
}

internal record EventDocument(
    [property: JsonPropertyName(EventMetadataNames.Id)] string Id,
    [property: JsonPropertyName(EventMetadataNames.PartitionKey)] string PartitionKey,
    [property: JsonPropertyName(EventMetadataNames.Properties)] EventMetadata Metadata,
    [property: JsonPropertyName(EventMetadataNames.Data)] object Data);
