using Chronicles.Documents;
using Chronicles.Documents.Internal;

namespace Microsoft.Extensions.DependencyInjection;

public class ChroniclesBuilder
{
    public ChroniclesBuilder(
        IServiceCollection services)
    {
        Services = services;
    }

    public IServiceCollection Services { get; }

    public ChroniclesBuilder AddDocumentStore(
        string storeName,
        Action<DocumentOptions> optionsProvider)
    {
        Services.Configure(storeName, optionsProvider);
        return this;
    }

    public ChroniclesBuilder AddDocumentStore(
        string storeName)
        => AddDocumentStore(storeName, options => { });

    public ChroniclesBuilder AddContainer<T>(
        string containerName,
        string? storeName = null)
        => AddContainer(typeof(T), containerName, storeName);

    public ChroniclesBuilder AddContainer(
        Type documentType,
        string containerName,
        string? storeName = null)
    {
        Services.AddSingleton(new ContainerNameRegistration(
            documentType,
            containerName,
            storeName));

        return this;
    }

    public ChroniclesBuilder AddSubscription<TDocument, TProcessor>(
        string subscriptionName)
        where TProcessor : class, IDocumentProcessor<TDocument>
        => AddSubscription<TDocument, TProcessor>(subscriptionName, o => { });

    public ChroniclesBuilder AddSubscription<TDocument, TProcessor>(
        string subscriptionName,
        Action<SubscriptionOptions> optionsProvider)
        where TProcessor : class, IDocumentProcessor<TDocument>
    {
        Services.Configure(subscriptionName, optionsProvider);
        Services.AddSingleton<TProcessor>();

        Services.AddSingleton<IDocumentSubscription>(s =>
            new DocumentSubscription<TDocument, TProcessor>(
                subscriptionName,
                s.GetRequiredService<IChangeFeedFactory>(),
                s.GetRequiredService<TProcessor>()));

        return this;
    }
}
