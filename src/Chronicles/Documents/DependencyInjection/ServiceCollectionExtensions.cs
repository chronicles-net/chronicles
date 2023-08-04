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
            .AddSingleton<ICosmosContainerProvider, CosmosContainerProvider>()
            .AddSingleton<ICosmosLinqQuery, CosmosLinqQuery>()
            .AddSingleton<IChangeFeedFactory, ChangeFeedFactory>()
            .AddSingleton<ISubscriptionManager, SubscriptionManager>();

        return new ChroniclesBuilder(services);
    }

    public static ChroniclesBuilder AddChronicles(
        this IServiceCollection services)
        => AddChronicles(services, options => { });
}

public class ChroniclesBuilder
{
    private readonly IServiceCollection services;

    public ChroniclesBuilder(
        IServiceCollection services)
    {
        this.services = services;
    }

    public ChroniclesBuilder AddDocumentStore(
        string storeName,
        Action<DocumentOptions> optionsProvider)
    {
        services.Configure(storeName, optionsProvider);
        return this;
    }

    public ChroniclesBuilder AddDocumentStore(
        string storeName)
        => AddDocumentStore(storeName, options => { });

    public ChroniclesBuilder AddSubcription<TDocument, TProcessor>(
        string name)
        where TProcessor : class, IDocumentProcessor<TDocument>
        => AddSubcription<TDocument, TProcessor>(name, o => { });

    public ChroniclesBuilder AddSubcription<TDocument, TProcessor>(
        string subscriptionName,
        Action<SubscriptionOptions> optionsProvider)
        where TProcessor : class, IDocumentProcessor<TDocument>
    {
        services.Configure(subscriptionName, optionsProvider);
        services.AddSingleton<TProcessor>();

        services.AddSingleton<IDocumentSubscription>(s =>
            new DocumentSubscription<TDocument, TProcessor>(
                subscriptionName,
                s.GetRequiredService<IChangeFeedFactory>(),
                s.GetRequiredService<TProcessor>()));

        return this;
    }
}
