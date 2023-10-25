namespace Chronicles.Documents;

public class SubscriptionOptions
{
    /// <summary>
    /// Sets the maximum number of items to receive in a batch from the subscription.
    /// </summary>
    public int BatchSize { get; set; } = 100;

    public bool ForceSingleInstance { get; set; }

    public TimeSpan PollingInterval { get; set; } = TimeSpan.FromSeconds(1);

    public SubscriptionStartOptions StartOptions { get; set; } = SubscriptionStartOptions.FromBeginning;
}