using Chronicles.Documents.Internal;
using Microsoft.Extensions.DependencyInjection;

namespace Chronicles.Documents.DependencyInjection;

public class DocumentStoreBuilder
{
    public DocumentStoreBuilder(
        string storeName,
        IServiceCollection services)
    {
        StoreName = storeName;
        Services = services;
    }

    public string StoreName { get; }

    public IServiceCollection Services { get; }

    public DocumentStoreBuilder Configure(
        Action<DocumentOptions> optionsProvider)
    {
        Services.Configure(StoreName, optionsProvider);

        return this;
    }

    public DocumentStoreBuilder AddSubscription<TDocument, TProcessor>(
        string subscriptionName)
        where TProcessor : class, IDocumentProcessor<TDocument>
        where TDocument : class
        => AddSubscription<TDocument, TProcessor>(subscriptionName, o => { });

    public DocumentStoreBuilder AddSubscription<TDocument, TProcessor>(
        string subscriptionName,
        Action<SubscriptionOptions> optionsProvider)
        where TProcessor : class, IDocumentProcessor<TDocument>
        where TDocument : class
    {
        Services.Configure(subscriptionName, optionsProvider);
        Services.AddSingleton<TProcessor>();

        Services.AddSingleton<IDocumentSubscription>(s =>
            new DocumentSubscription<TDocument, TProcessor>(
                StoreName,
                subscriptionName,
                s.GetRequiredService<IChangeFeedFactory>(),
                s.GetRequiredService<TProcessor>()));

        return this;
    }
}
