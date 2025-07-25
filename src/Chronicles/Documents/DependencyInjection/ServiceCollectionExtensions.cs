using Chronicles.Documents;
using Chronicles.Documents.DependencyInjection;
using Chronicles.Documents.Internal;
using Chronicles.Documents.Testing;
using Microsoft.Extensions.Options;

#pragma warning disable IDE0130 // Namespace does not match folder structure
namespace Microsoft.Extensions.DependencyInjection;
#pragma warning restore IDE0130 // Namespace does not match folder structure

public static class ServiceCollectionExtensions
{
    public static ChroniclesBuilder AddChronicles(
        this IServiceCollection services,
        Action<DocumentStoreBuilder>? builder = null)
    {
        builder?.Invoke(new(DocumentOptions.DefaultStoreName, services));
        services.AddSingleton<IDocumentStore>(s => new DocumentStore(
            DocumentOptions.DefaultStoreName,
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

    public static ChroniclesBuilder AddFakeChronicles(
        this IServiceCollection services,
        Action<DocumentStoreBuilder>? builder = null)
    {
        builder?.Invoke(new(DocumentOptions.DefaultStoreName, services));
        services.AddSingleton<IDocumentStore>(s => new DocumentStore(
            DocumentOptions.DefaultStoreName,
            s.GetRequiredService<IOptionsMonitor<DocumentOptions>>()));

        services
            .AddSingleton(typeof(IDocumentReader<>), typeof(FakeDocumentReader<>))
            .AddSingleton(typeof(IDocumentWriter<>), typeof(FakeDocumentWriter<>))
            .AddSingleton<IFakeDocumentStoreProvider, FakeDocumentStoreProvider>()
            .AddSingleton<IContainerNameRegistry, ContainerNameRegistry>()
            .AddSingleton<IChangeFeedFactory, FakeChangeFeedFactory>()
            .AddSingleton<ISubscriptionManager, SubscriptionManager>()
            .AddSingleton<IDocumentStoreInitializer, FakeDocumentStoreInitializer>()
            .AddHostedService<DocumentStoreService>();

        return new ChroniclesBuilder(services);
    }
}
