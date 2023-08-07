using Chronicles.Documents;
using Chronicles.Documents.Internal;

namespace Microsoft.Extensions.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static ChroniclesBuilder AddChronicles(
        this IServiceCollection services,
        Action<DocumentOptions> optionsProvider)
    {
        services.Configure(optionsProvider);

        var registry = new ContainerNameRegistry();
        services
            .AddSingleton(typeof(IDocumentReader<>), typeof(CosmosReader<>))
            .AddSingleton(typeof(IDocumentWriter<>), typeof(CosmosWriter<>))
            .AddSingleton<ICosmosClientProvider, CosmosClientProvider>()
            .AddSingleton<ICosmosSerializerProvider, CosmosSerializerProvider>()
            .AddSingleton<IContainerNameRegistry>(registry)
            .AddSingleton<ICosmosContainerProvider, CosmosContainerProvider>()
            .AddSingleton<ICosmosLinqQuery, CosmosLinqQuery>()
            .AddSingleton<IChangeFeedFactory, ChangeFeedFactory>()
            .AddSingleton<ISubscriptionManager, SubscriptionManager>()
            .AddHostedService<DocumentStoreService>();

        return new ChroniclesBuilder(services, registry);
    }

    public static ChroniclesBuilder AddChronicles(
        this IServiceCollection services)
        => AddChronicles(services, options => { });
}
