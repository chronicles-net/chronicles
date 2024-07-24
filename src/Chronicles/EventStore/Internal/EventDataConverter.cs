using System.Text.Json;

namespace Chronicles.EventStore.Internal;

internal class EventDataConverter(
    string eventName,
    Type eventType)
    : IEventDataConverter
{
    public virtual object? Convert(
        EventConverterContext context)
        => context.Metadata.Name == eventName
         ? context.Data.Deserialize(
             eventType,
             context.Options)
         : null;
}