using Microsoft.Extensions.Options;

namespace Chronicles.EventStore;

public class EventStoreOptions
{
    private readonly Dictionary<Type, string> eventNames = new();

    public string DocumentStoreName { get; set; } = Options.DefaultName;

    public string EventStoreContainer { get; set; } = "event-store";

    public string StreamIndexContainer { get; set; } = "stream-index";

    public IReadOnlyDictionary<Type, string> EventNames => eventNames;

    public EventStoreOptions UseEventStoreContainerName(
        string containerName)
    {
        EventStoreContainer = containerName;
        return this;
    }

    public EventStoreOptions UseStreamIndexContainerName(
        string containerName)
    {
        StreamIndexContainer = containerName;
        return this;
    }

    public EventStoreOptions AddEvent<T>()
        => AddEvent(typeof(T), string.Empty);

    public EventStoreOptions AddEvent<T>(string name)
        => AddEvent(typeof(T), name);

    public EventStoreOptions AddEvent(Type type, string name)
    {
        eventNames[type] = name;
        return this;
    }
}