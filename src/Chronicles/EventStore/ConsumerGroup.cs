namespace Chronicles.EventStore;

public class ConsumerGroup
{
    internal ConsumerGroup(
        string name,
        string instance,
        TimeSpan pollingInterval)
    {
        Name = name;
        Instance = instance;
        PollingInterval = pollingInterval;
    }

    public string Name { get; }

    public string Instance { get; }

    public TimeSpan PollingInterval { get; }

    /// <summary>
    /// Creates a named consumer group with a unique instance
    /// name use for auto scaling across multiple processes.
    /// </summary>
    /// <remarks>
    /// Any changes to a given stream is guaranteed to be processed by the same consumer.
    /// </remarks>
    /// <param name="name">Name of the consumer group.</param>
    /// <param name="pollingInterval">The delay in between polling the change feed for new changes, after all current changes are drained.</param>
    /// <returns>An instance of <see cref="ConsumerGroup"/>.</returns>
    public static ConsumerGroup Create(
        string name,
        TimeSpan pollingInterval)
        => new(
            name,
            Guid.NewGuid().ToString(),
            pollingInterval);
}