using System.Text.Json;

namespace Chronicles.EventStore.Internal.Events;

public class SingleEventDataConverter :
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

    public virtual object? Convert(
        EventConverterContext context)
        => context.Metadata.Name == name
         ? context.Data.Deserialize(
             eventType,
             context.Options)
         : null;

    public virtual EventName GetName(Type type)
        => eventType == type
         ? name
         : EventName.Unknown;
}