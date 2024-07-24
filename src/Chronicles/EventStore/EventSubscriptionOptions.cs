using Chronicles.Documents;

namespace Chronicles.EventStore;

public class EventSubscriptionOptions
{
    public SubscriptionOptions SubscriptionOptions { get; } = new();
}