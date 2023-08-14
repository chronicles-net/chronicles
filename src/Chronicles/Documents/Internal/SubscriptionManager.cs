namespace Chronicles.Documents.Internal;

public class SubscriptionManager : ISubscriptionManager
{
    private readonly IEnumerable<IDocumentSubscription> subscriptions;

    public SubscriptionManager(
        IEnumerable<IDocumentSubscription> subscriptions)
    {
        this.subscriptions = subscriptions;
    }

    public IDocumentSubscription GetSubscriptions(string subscriptionName)
        => subscriptions.FirstOrDefault(s => s.SubscriptionName == subscriptionName);

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        foreach (var subscription in subscriptions)
        {
            await subscription.StartAsync(cancellationToken).ConfigureAwait(false);
        }
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        foreach (var subscription in subscriptions)
        {
            await subscription.StopAsync(cancellationToken).ConfigureAwait(false);
        }
    }
}
