namespace Chronicles.Documents.Internal;

public interface IDocumentSubscription
{
    string SubscriptionName { get; }

    Task StartAsync(CancellationToken cancellationToken);

    Task StopAsync(CancellationToken cancellationToken);
}
