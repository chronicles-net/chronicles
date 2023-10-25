namespace Chronicles.EventStore.Internal.Events;

/// <summary>
/// Represents registry of events configured for a given store.
/// </summary>
public class EventRegistry
{
    private readonly object syncLock = new();
    private readonly EventStoreOptions options;
    private Dictionary<Type, SingleEventDataConverter>? types;
    private Dictionary<string, SingleEventDataConverter>? names;

    public EventRegistry(
        EventStoreOptions options)
        => this.options = options;

    public virtual IEventDataConverter? GetConverter(
        string eventName)
    {
        EnsureInitialized();

        if (names!.TryGetValue(eventName, out var converter))
        {
            return converter;
        }

        return null;
    }

    public virtual string GetEventName(Type eventType)
    {
        EnsureInitialized();

        if (types!.TryGetValue(eventType, out var converter))
        {
            return converter.EventName;
        }

        throw new ArgumentException(
            $"Event of type '{eventType}' is not registered in event store options.",
            nameof(eventType));
    }

    // As this class is instantiated from an option builder
    // we need to ensure we lazy load converters from configurations
    // options, so we have the latest configured values.
    private void EnsureInitialized()
    {
        if (names is not null)
        {
            return;
        }

        lock (syncLock)
        {
            if (names is not null)
            {
                return;
            }

            var tempNames = new Dictionary<string, SingleEventDataConverter>();
            var tempTypes = new Dictionary<Type, SingleEventDataConverter>();

            if (options.EventNames.Any())
            {
                foreach (var eventName in options.EventNames)
                {
                    tempTypes[eventName.Key] = new SingleEventDataConverter(eventName.Value, eventName.Key);
                    tempNames[eventName.Value] = tempTypes[eventName.Key];
                }
            }

            names = tempNames;
            types = tempTypes;
        }
    }
}
