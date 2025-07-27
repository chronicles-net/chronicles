namespace Chronicles.Documents.Internal;

internal class SubscriptionService(
    IEnumerable<IDocumentSubscription> subscriptions)
    : ISubscriptionService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        foreach (var subscription in subscriptions)
        {
            await subscription
                .StartAsync(cancellationToken)
                .ConfigureAwait(false);
        }
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        foreach (var subscription in subscriptions)
        {
            await subscription
                .StopAsync(cancellationToken)
                .ConfigureAwait(false);
        }
    }
}
