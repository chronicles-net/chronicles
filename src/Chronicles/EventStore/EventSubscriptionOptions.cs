using Chronicles.Documents;

namespace Chronicles.EventStore;

public class EventSubscriptionOptions
{
    public SubscriptionOptions SubscriptionOptions { get; } = new();

    public Func<StreamEvent, bool> Filter { get; set; } = _ => true;

    public bool StopOnException { get; set; }

    public EventPartitioningStrategy Strategy { get; set; }
        = EventPartitioningStrategy.EventId;
}
