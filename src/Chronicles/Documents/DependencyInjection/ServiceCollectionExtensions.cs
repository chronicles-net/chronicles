using Chronicles.Documents;
using Chronicles.Documents.Internal;
using Microsoft.Extensions.Options;

namespace Microsoft.Extensions.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static ChroniclesBuilder AddChronicles(
        this IServiceCollection services,
        Action<DocumentOptions> optionsProvider)
    {
        services
            .Configure(optionsProvider)
            .AddSingleton<IDocumentStore>(s => new DocumentStore(
                Options.Options.DefaultName,
                s.GetRequiredService<IOptionsMonitor<DocumentOptions>>()));

        services
            .AddSingleton(typeof(IDocumentReader<>), typeof(CosmosReader<>))
            .AddSingleton(typeof(IDocumentWriter<>), typeof(CosmosWriter<>))
            .AddSingleton<ICosmosClientProvider, CosmosClientProvider>()
            .AddSingleton<IContainerNameRegistry, ContainerNameRegistry>()
            .AddSingleton<ICosmosContainerProvider, CosmosContainerProvider>()
            .AddSingleton<ICosmosLinqQuery, CosmosLinqQuery>()
            .AddSingleton<IChangeFeedFactory, ChangeFeedFactory>()
            .AddSingleton<ISubscriptionManager, SubscriptionManager>()
            .AddSingleton<IDocumentStoreInitializer, DocumentStoreInitializer>()
            .AddHostedService<DocumentStoreService>();

        return new ChroniclesBuilder(services);
    }

    public static ChroniclesBuilder AddChronicles(
        this IServiceCollection services)
        => AddChronicles(services, options => { });
}
