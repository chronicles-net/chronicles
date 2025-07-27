using Chronicles.Documents.Internal;
using Microsoft.Extensions.DependencyInjection;

namespace Chronicles.Documents.DependencyInjection;

/// <summary>
/// Provides a builder for configuring document stores and subscriptions in Chronicles.
/// Use this class to register document store options, subscriptions, and processors for event sourcing and projections.
/// </summary>
public class DocumentStoreBuilder
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DocumentStoreBuilder"/> class.
    /// </summary>
    /// <param name="storeName">The name of the document store.</param>
    /// <param name="services">The service collection for dependency injection.</param>
    public DocumentStoreBuilder(
        string storeName,
        IServiceCollection services)
    {
        StoreName = storeName;
        Services = services;
    }

    /// <summary>
    /// Gets the name of the document store being configured.
    /// </summary>
    public string StoreName { get; }

    /// <summary>
    /// Gets the service collection used for dependency injection.
    /// </summary>
    public IServiceCollection Services { get; }

    /// <summary>
    /// Configures document store options for the specified store.
    /// Use this method to set connection, database, container, and serialization options for the document store.
    /// </summary>
    /// <param name="optionsProvider">An action to configure <see cref="DocumentOptions"/> for the store.</param>
    /// <returns>The updated <see cref="DocumentStoreBuilder"/> instance for chaining.</returns>
    public DocumentStoreBuilder Configure(
        Action<DocumentOptions> optionsProvider)
    {
        Services.Configure(StoreName, optionsProvider);

        return this;
    }

    /// <summary>
    /// Registers a subscription for the specified document and processor types with default options.
    /// Use this method to enable change feed processing for a document type, allowing you to react to changes in the store.
    /// </summary>
    /// <typeparam name="TDocument">The type of the document to subscribe to.</typeparam>
    /// <typeparam name="TProcessor">The type of the processor that handles document changes.</typeparam>
    /// <param name="subscriptionName">The name of the subscription.</param>
    /// <returns>The updated <see cref="DocumentStoreBuilder"/> instance for chaining.</returns>
    public DocumentStoreBuilder AddSubscription<TDocument, TProcessor>(
        string subscriptionName)
        where TProcessor : class, IDocumentProcessor<TDocument>
        where TDocument : class
        => AddSubscription<TDocument, TProcessor>(subscriptionName, o => { });

    /// <summary>
    /// Registers a subscription for the specified document and processor types with custom options.
    /// Use this method to enable change feed processing for a document type and configure subscription behavior, such as batch size and polling interval.
    /// </summary>
    /// <typeparam name="TDocument">The type of the document to subscribe to.</typeparam>
    /// <typeparam name="TProcessor">The type of the processor that handles document changes.</typeparam>
    /// <param name="subscriptionName">The name of the subscription.</param>
    /// <param name="optionsProvider">An action to configure <see cref="SubscriptionOptions"/> for the subscription.</param>
    /// <returns>The updated <see cref="DocumentStoreBuilder"/> instance for chaining.</returns>
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
