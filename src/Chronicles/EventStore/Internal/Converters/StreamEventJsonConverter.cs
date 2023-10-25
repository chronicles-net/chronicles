using System.Text.Json;
using System.Text.Json.Serialization;
using Chronicles.EventStore.Internal.Events;
using Chronicles.EventStore.Internal.Streams;

namespace Chronicles.EventStore.Internal.Converters;

/// <summary>
/// Responsible for converting an event envelope to and from json without loosing underlying event type.
/// </summary>
internal sealed class StreamEventJsonConverter : JsonConverter<StreamEvent>
{
    private readonly StreamEventConverter dataConverter;

    public StreamEventJsonConverter(
        StreamEventConverter dataConverter)
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
        if (jsonDocument.RootElement.TryGetProperty(JsonPropertyNames.Id, out var id)
            && id.GetString() == JsonPropertyNames.StreamMetadataId)
        {
            return new StreamEvent(
                jsonDocument.Deserialize<StreamMetadataDocument>(options)!,
                EventMetadata.StreamMetadata);
        }

        if (jsonDocument.RootElement.TryGetProperty(JsonPropertyNames.Properties, out var properties))
        {
            try
            {
                var metadata = properties.Deserialize<EventMetadata>(options);
                if (metadata is not null && jsonDocument.RootElement.TryGetProperty(JsonPropertyNames.Data, out var data))
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