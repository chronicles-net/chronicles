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

        services
            .AddSingleton(typeof(IDocumentReader<>), typeof(CosmosReader<>))
            .AddSingleton(typeof(IDocumentWriter<>), typeof(CosmosWriter<>))
            .AddSingleton<ICosmosClientProvider, CosmosClientProvider>()
            .AddSingleton<ICosmosSerializerProvider, CosmosSerializerProvider>()
            .AddSingleton<IContainerNameRegistry, ContainerNameRegistry>()
            .AddSingleton<ICosmosContainerProvider, CosmosContainerProvider>()
            .AddSingleton<ICosmosLinqQuery, CosmosLinqQuery>()
            .AddSingleton<IChangeFeedFactory, ChangeFeedFactory>()
            .AddSingleton<ISubscriptionManager, SubscriptionManager>()
            .AddHostedService<DocumentStoreService>();

        return new ChroniclesBuilder(services);
    }

    public static ChroniclesBuilder AddChronicles(
        this IServiceCollection services)
        => AddChronicles(services, options => { });
}
