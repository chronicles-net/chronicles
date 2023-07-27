using System.Text.Json;

namespace Chronicles.EventStore.Events;

public sealed class SingleEventDataConverter :
    IEventDataConverter
{
    private readonly EventName name;
    private readonly Type eventType;

    public SingleEventDataConverter(
        EventName name,
        Type eventType)
    {
        this.name = name;
        this.eventType = eventType;
    }

    public object? Convert(
        EventConverterContext context)
        => context.Metadata.Name == name
         ? JsonSerializer.Deserialize(
             context.Data,
             eventType,
             context.Options)
         : null;
}