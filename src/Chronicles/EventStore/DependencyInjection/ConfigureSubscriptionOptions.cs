using Chronicles.Documents;
using Microsoft.Extensions.Options;

namespace Chronicles.EventStore.DependencyInjection;

internal class ConfigureSubscriptionOptions
    : IConfigureNamedOptions<SubscriptionOptions>
{
    private readonly IOptionsMonitor<EventSubscriptionOptions> eventSubscriptionOptions;

    public ConfigureSubscriptionOptions(
        IOptionsMonitor<EventSubscriptionOptions> eventSubscriptionOptions)
        => this.eventSubscriptionOptions = eventSubscriptionOptions;

    public void Configure(
        SubscriptionOptions options)
        => Configure(DocumentOptions.DefaultStoreName, options);

    public void Configure(
        string? name,
        SubscriptionOptions options)
    {
        var eventSubscription = eventSubscriptionOptions.Get(name);

        options.BatchSize = eventSubscription.SubscriptionOptions.BatchSize;
        options.ForceSingleInstance = eventSubscription.SubscriptionOptions.ForceSingleInstance;
        options.PollingInterval = eventSubscription.SubscriptionOptions.PollingInterval;
        options.StartOptions = eventSubscription.SubscriptionOptions.StartOptions;
    }
}
