namespace Chronicles.Documents.Internal;

internal interface IDocumentSubscription
{
    string SubscriptionName { get; }

    Task StartAsync(CancellationToken cancellationToken);

    Task StopAsync(CancellationToken cancellationToken);
}
