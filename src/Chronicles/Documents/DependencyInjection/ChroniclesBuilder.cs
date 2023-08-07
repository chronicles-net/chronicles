using Chronicles.Documents;
using Chronicles.Documents.Internal;
using Microsoft.Extensions.Options;

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
        Services.AddSingleton<IDocumentStore>(s => new DocumentStore(
            storeName,
            s.GetRequiredService<IOptionsMonitor<DocumentOptions>>()));

        return this;
    }

    public ChroniclesBuilder AddDocumentStore(
        string storeName)
        => AddDocumentStore(storeName, options => { });

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
