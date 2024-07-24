namespace Chronicles.EventStore;

public interface IEventCatalog
{
    IEventDataConverter? GetConverter(
        string eventName);

    string GetEventName(
        Type eventType);
}