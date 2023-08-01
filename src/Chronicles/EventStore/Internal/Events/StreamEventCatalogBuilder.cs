namespace Chronicles.EventStore.Internal.Events;

public class StreamEventCatalogBuilder
{
    private readonly Dictionary<EventName, IEventDataConverter> converters = new();

    public StreamEventCatalogBuilder RegisterEventType(
        EventName name,
        Type type)
    {
        converters[name] = new SingleEventDataConverter(name, type);
        return this;
    }

    public StreamEventCatalog Build()
        => new(converters.Values.ToArray());
}