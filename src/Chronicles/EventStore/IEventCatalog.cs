namespace Chronicles.EventStore;

/// <summary>
/// Defines a contract for an event catalog that provides event data converters and event names.
/// </summary>
public interface IEventCatalog
{
    /// <summary>
    ///   Retrieves the event data converter for the specified event name.
    /// </summary>
    /// <param name="eventName">The name of the event.</param>
    /// <returns>
    ///   An instance of <see cref="IEventDataConverter"/> if a converter is found; otherwise, <c>null</c>.
    /// </returns>
    IEventDataConverter? GetConverter(string eventName);

    /// <summary>
    ///   Retrieves the event name for the specified event type.
    /// </summary>
    /// <param name="eventType">The type of the event.</param>
    /// <returns>The name of the event.</returns>
    string GetEventName(Type eventType);
}
