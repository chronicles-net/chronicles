namespace Chronicles.EventStore.Internal;

internal class EventCatalog : IEventCatalog
{
    private readonly Dictionary<Type, (string Name, IEventDataConverter Converter)> types;
    private readonly Dictionary<string, (string Name, IEventDataConverter Converter)> names;

    public EventCatalog(
        IDictionary<Type, (string Name, IEventDataConverter Converter)> typeToNameMappings)
    {
        types = typeToNameMappings
            .ToDictionary(
                kvp => kvp.Key,
                kvp => kvp.Value);

        names = typeToNameMappings
            .ToDictionary(
                kvp => kvp.Value.Name,
                kvp => types[kvp.Key]);
    }

    public IEventDataConverter? GetConverter(
        string eventName)
        => names.TryGetValue(eventName, out var item)
            ? item.Converter
            : null;

    public string GetEventName(Type eventType)
        => types.TryGetValue(eventType, out var item)
            ? item.Name
            : throw new ArgumentException(
                $"Event of type '{eventType}' is not registered in event catalog.",
                nameof(eventType));
}