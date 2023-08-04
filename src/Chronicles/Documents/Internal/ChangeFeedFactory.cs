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
        string subscriptionName,
        Container.ChangesHandler<T> onChanges,
        Container.ChangeFeedMonitorErrorDelegate? onError = null)
    {
        var options = subscriptionOptions.Get(subscriptionName);
        var container = containerProvider.GetContainer<T>();

        var builder = container
            .GetChangeFeedProcessorBuilder(
                subscriptionName,
                onChanges)
            .WithLeaseContainer(
                containerProvider.GetSubscriptionContainer<T>())
            .WithMaxItems(100)
            .WithPollInterval(options.PollingInterval)
            .WithStartTime(DateTime.MinValue.ToUniversalTime()); // Will start from the beginning of feed when no lease is found.

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
