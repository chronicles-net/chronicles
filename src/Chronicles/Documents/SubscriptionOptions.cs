namespace Chronicles.Documents;

public class SubscriptionOptions
{
    public bool ForceSingleInstance { get; set; }

    public TimeSpan PollingInterval { get; set; } = TimeSpan.FromSeconds(1);

    public SubscriptionStartOptions StartOptions { get; set; } = SubscriptionStartOptions.FromBeginning;
}