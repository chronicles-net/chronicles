namespace Chronicles.EventStore.Events;

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

    public StreamEventConverter Build()
        => new(converters.Values.ToArray());
}