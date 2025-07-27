using Chronicles.Documents;
using Chronicles.Documents.DependencyInjection;
using Chronicles.Documents.Internal;
using Microsoft.Extensions.Options;

#pragma warning disable IDE0130 // Namespace does not match folder structure
namespace Microsoft.Extensions.DependencyInjection;
#pragma warning restore IDE0130 // Namespace does not match folder structure

/// <summary>
/// Provides extension methods for configuring Chronicles document stores and related services in the dependency injection container.
/// Use these methods to register Cosmos DB document storage, readers, writers, and supporting infrastructure for event sourcing and projections.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds and configures Chronicles document store services to the dependency injection container.
    /// Use this method to enable Cosmos DB-backed document store, event sourcing, and projections in your application.
    /// </summary>
    /// <param name="services">The service collection to configure.</param>
    /// <param name="builder">Optional action to further configure the document store using a <see cref="DocumentStoreBuilder"/>.</param>
    /// <returns>A <see cref="ChroniclesBuilder"/> for further configuration.</returns>
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
            .AddSingleton<ISubscriptionService, SubscriptionService>()
            .AddSingleton<IDocumentStoreInitializer, DocumentStoreInitializer>()
            .AddHostedService<DocumentStoreService>();

        return new ChroniclesBuilder(services);
    }
}
