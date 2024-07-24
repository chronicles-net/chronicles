using Microsoft.Azure.Cosmos;

namespace Chronicles.Documents.Internal;

public class DocumentSubscription<TDocument, TProcessor> : IDocumentSubscription
    where TProcessor : IDocumentProcessor<TDocument>
{
    private readonly ChangeFeedProcessor changeFeed;

    public DocumentSubscription(
        string storeName,
        string subscriptionName,
        IChangeFeedFactory changeFeedFactory,
        TProcessor processor)
    {
        SubscriptionName = subscriptionName;
        this.changeFeed = changeFeedFactory
            .Create<TDocument>(
                storeName,
                subscriptionName,
                processor.ProcessAsync,
                processor.ErrorAsync);
    }

    public string SubscriptionName { get; }

    public Task StartAsync(CancellationToken cancellationToken)
        => changeFeed.StartAsync();

    public Task StopAsync(CancellationToken cancellationToken)
        => changeFeed.StopAsync();
}