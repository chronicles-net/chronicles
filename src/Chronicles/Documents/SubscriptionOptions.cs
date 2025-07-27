namespace Chronicles.Documents;

/// <summary>
/// Provides configuration options for document subscriptions in Chronicles.
/// Use this class to control batching, polling, instance behavior, and starting position for change feed subscriptions.
/// </summary>
public class SubscriptionOptions
{
    /// <summary>
    /// Gets or sets the maximum number of items to receive in a batch from the subscription.
    /// Adjust this value to control throughput and memory usage when processing changes.
    /// </summary>
    public int BatchSize { get; set; } = 100;

    /// <summary>
    /// Gets or sets a value indicating whether only a single instance of the subscription should be allowed.
    /// Set to <c>true</c> to enforce that only one processor instance runs for this subscription.
    /// </summary>
    public bool ForceSingleInstance { get; set; }

    /// <summary>
    /// Gets or sets the interval between polling for new changes in the subscription.
    /// Adjust this value to control how frequently the subscription checks for updates.
    /// </summary>
    public TimeSpan PollingInterval { get; set; } = TimeSpan.FromSeconds(1);

    /// <summary>
    /// Gets or sets the starting position for the subscription when it is first started.
    /// Use <see cref="SubscriptionStartOptions.FromBeginning"/> to start from the beginning of the change feed, or <see cref="SubscriptionStartOptions.FromNow"/> to start from the current time.
    /// </summary>
    public SubscriptionStartOptions StartOptions { get; set; } = SubscriptionStartOptions.FromBeginning;
}
