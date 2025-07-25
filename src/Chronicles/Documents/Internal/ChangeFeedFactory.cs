using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Options;

namespace Chronicles.Documents.Internal;

public class ChangeFeedFactory : IChangeFeedFactory
{
    private readonly ICosmosContainerProvider containerProvider;
    private readonly IOptionsMonitor<SubscriptionOptions> subscriptionOptions;

    public ChangeFeedFactory(
        ICosmosContainerProvider containerProvider,
        IOptionsMonitor<SubscriptionOptions> subscriptionOptions)
    {
        this.containerProvider = containerProvider;
        this.subscriptionOptions = subscriptionOptions;
    }

    public ChangeFeedProcessor Create<T>(
        string? storeName,
        string subscriptionName,
        Container.ChangesHandler<T> onChanges,
        Container.ChangeFeedMonitorErrorDelegate? onError = null)
        where T : class
    {
        var options = subscriptionOptions.Get(subscriptionName);
        var container = containerProvider.GetContainer<T>(storeName);

        var builder = container
            .GetChangeFeedProcessorBuilder(
                subscriptionName,
                onChanges)
            .WithLeaseContainer(
                containerProvider.GetSubscriptionContainer(storeName))
            .WithMaxItems(options.BatchSize)
            .WithPollInterval(options.PollingInterval);

        if (options.StartOptions == SubscriptionStartOptions.FromBeginning)
        {
            // Instruct processor to start from beginning.
            // see https://docs.microsoft.com/en-us/azure/cosmos-db/change-feed-processor#reading-from-the-beginning
            builder.WithStartTime(DateTime.MinValue.ToUniversalTime());
        }

        if (!options.ForceSingleInstance)
        {
            builder.WithInstanceName(Guid.NewGuid().ToString());
        }

        if (onError != null)
        {
            builder = builder.WithErrorNotification(onError);
        }

        return builder.Build();
    }
}
