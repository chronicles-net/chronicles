using System.Text.Json;
using System.Text.Json.Serialization;
using Chronicles.EventStore.Internal.Events;
using Chronicles.EventStore.Internal.Streams;

namespace Chronicles.EventStore.Internal.Converters;

internal class StreamEventDocumentJsonConverter : JsonConverter<StreamEventDocument>
{
    private readonly EventRegistry eventRegistry;
    private JsonSerializerOptions? optionsWithoutConverter;

    public StreamEventDocumentJsonConverter(
        EventRegistry eventRegistry)
        => this.eventRegistry = eventRegistry;

    public override StreamEventDocument Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options)
        => throw new NotImplementedException();

    public override void Write(
        Utf8JsonWriter writer,
        StreamEventDocument value,
        JsonSerializerOptions options)
    {
        optionsWithoutConverter ??= CopyAndRemoveConverter(
            new JsonSerializerOptions(options),
            typeof(StreamEventDocumentJsonConverter));

        JsonSerializer
            .Serialize(
                writer,
                value with
                {
                    Properties = value.Properties with
                    {
                        Name = eventRegistry.GetEventName(value.Data.GetType()),
                    },
                },
                optionsWithoutConverter);
    }

    public static JsonSerializerOptions CopyAndRemoveConverter(
        JsonSerializerOptions options,
        Type converterType)
    {
        var copy = new JsonSerializerOptions(options);
        for (var i = copy.Converters.Count - 1; i >= 0; i--)
        {
            if (copy.Converters[i].GetType() == converterType)
            {
                copy.Converters.RemoveAt(i);
            }
        }

        return copy;
    }
}