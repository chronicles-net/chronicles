using Microsoft.Extensions.Hosting;

namespace Chronicles.Documents.Internal;

public class DocumentStoreService : IHostedService
{
    private readonly ISubscriptionManager subscriptionManager;

    public DocumentStoreService(
        ISubscriptionManager subscriptionManager)
    {
        this.subscriptionManager = subscriptionManager;
    }

    public Task StartAsync(CancellationToken cancellationToken)
        => subscriptionManager.StartAsync(cancellationToken);

    public Task StopAsync(CancellationToken cancellationToken)
        => subscriptionManager.StopAsync(cancellationToken);
}
