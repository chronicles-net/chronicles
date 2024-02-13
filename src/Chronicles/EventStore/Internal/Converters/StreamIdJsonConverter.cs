using System.Text.Json;
using System.Text.Json.Serialization;

namespace Chronicles.EventStore.Internal.Converters;

internal class StreamIdJsonConverter : JsonConverter<StreamId>
{
    public override StreamId Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options)
        => reader.GetString() switch
        {
            { } id => StreamId.FromString(id),
            _ => throw new JsonException(),
        };

    public override void Write(
        Utf8JsonWriter writer,
        StreamId value,
        JsonSerializerOptions options)
        => writer.WriteStringValue((string)value);
}