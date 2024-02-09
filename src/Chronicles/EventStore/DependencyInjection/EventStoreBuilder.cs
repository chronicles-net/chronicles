using Chronicles.Documents;
using Chronicles.Documents.Internal;
using Chronicles.EventStore;
using Chronicles.EventStore.DependencyInjection;
using Chronicles.EventStore.Internal.EventConsumers;
using Chronicles.EventStore.Internal.Processors;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace Microsoft.Extensions.DependencyInjection;

public class EventStoreBuilder
{
    private readonly DocumentStoreBuilder documentBuilder;

    public EventStoreBuilder(
        DocumentStoreBuilder documentBuilder)
    {
        this.documentBuilder = documentBuilder;
    }

    public IServiceCollection Services => documentBuilder.Services;

    public string StoreName => documentBuilder.StoreName;

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

        Services.ConfigureOptions<ConfigureSubscriptionOptions>();
        Services.Configure<SubscriptionOptions>(name, o => { });
        Services.TryAddKeyedSingleton(name, (s, n) =>
            new EventDocumentProcessor(
                name,
                s.GetRequiredService<IEventConsumerFactory>(),
                s.GetRequiredService<IOptionsMonitor<EventSubscriptionOptions>>()
                 .Get(name)));

        Services.AddSingleton<IDocumentSubscription>(s =>
            new DocumentSubscription<StreamEvent, EventDocumentProcessor>(
                StoreName,
                name,
                s.GetRequiredService<IChangeFeedFactory>(),
                s.GetRequiredKeyedService<EventDocumentProcessor>(name)));

        return this;
    }
}
