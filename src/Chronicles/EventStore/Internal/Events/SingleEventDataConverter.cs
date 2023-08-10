using System.Text.Json;

namespace Chronicles.EventStore.Internal.Events;

public class SingleEventDataConverter :
    IEventDataConverter
{
    public SingleEventDataConverter(
        string eventName,
        Type eventType)
    {
        EventName = eventName;
        EventType = eventType;
    }

    public string EventName { get; }

    public Type EventType { get; }

    public virtual object? Convert(
        EventConverterContext context)
        => context.Metadata.Name == EventName
         ? context.Data.Deserialize(
             EventType,
             context.Options)
         : null;
}