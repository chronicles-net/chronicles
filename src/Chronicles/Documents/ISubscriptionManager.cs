using Chronicles.Documents.Internal;

namespace Chronicles.Documents;

public interface ISubscriptionManager
{
    IDocumentSubscription GetSubscriptions(string subscriptionName);

    Task StartAsync(CancellationToken cancellationToken);

    Task StopAsync(CancellationToken cancellationToken);
}
