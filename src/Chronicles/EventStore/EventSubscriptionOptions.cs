namespace Chronicles.EventStore;

public class EventSubscriptionOptions
{
    public string StreamFilter { get; set; } = "**";

    public bool StopOnException { get; set; }

    public int ConcurrencyLimit { get; set; } = 1;

    public EventPartitioningStrategy Strategy { get; set; }
        = EventPartitioningStrategy.EventId;
}
