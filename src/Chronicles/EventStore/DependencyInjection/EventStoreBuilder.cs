using Chronicles.EventStore;

namespace Microsoft.Extensions.DependencyInjection;

public class EventStoreBuilder
{
    public EventStoreBuilder(
        IServiceCollection serviceCollection,
        string storeName)
    {
        Services = serviceCollection;
        StoreName = storeName;
    }

    public IServiceCollection Services { get; }

    public string StoreName { get; }

    public EventStoreBuilder Configure(
        Action<EventStoreOptions> configure)
    {
        Services.Configure<EventStoreOptions>(
            StoreName,
            o =>
            {
                o.DocumentStoreName = StoreName;
                configure.Invoke(o);
            });
        Services.ConfigureOptions<EventStoreConfigureDocumentStore>();

        return this;
    }

    public EventStoreBuilder AddEventSubscription(
        string name,
        Action<EventSubscriptionBuilder> configure)
    {
        var builder = new EventSubscriptionBuilder(name, Services);
        configure.Invoke(builder);

        return this;
    }
}
