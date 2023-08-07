using Microsoft.Extensions.Hosting;

namespace Chronicles.Documents.Internal;

public class DocumentStoreService : IHostedService
{
    private readonly ISubscriptionManager subscriptionManager;
    private readonly IDocumentStoreInitializer initializer;

    public DocumentStoreService(
        ISubscriptionManager subscriptionManager,
        IDocumentStoreInitializer initializer)
    {
        this.subscriptionManager = subscriptionManager;
        this.initializer = initializer;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await initializer.InitializeAsync(cancellationToken).ConfigureAwait(false);
        await subscriptionManager.StartAsync(cancellationToken).ConfigureAwait(false);
    }

    public Task StopAsync(CancellationToken cancellationToken)
        => subscriptionManager.StopAsync(cancellationToken);
}
