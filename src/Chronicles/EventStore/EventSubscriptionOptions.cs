using Chronicles.Documents;

namespace Chronicles.EventStore;

/// <summary>
/// Provides configuration options for event subscriptions in Chronicles, including batching, polling, and starting position.
/// Use this class to control how event subscriptions process changes from the event store, such as batch size, polling interval, and where to start reading changes.
/// </summary>
public class EventSubscriptionOptions
{
    /// <summary>
    /// Gets the subscription options used to configure batching, polling, and starting position for the event subscription.
    /// Implement or set these options to control how the subscription processes changes from the event store.
    /// </summary>
    public SubscriptionOptions SubscriptionOptions { get; } = new();
}
