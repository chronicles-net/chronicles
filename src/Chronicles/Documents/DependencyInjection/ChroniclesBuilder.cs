using Chronicles.Documents.Internal;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Chronicles.Documents.DependencyInjection;

/// <summary>
/// Provides a builder for configuring document stores and related services in Chronicles.
/// </summary>
public class ChroniclesBuilder(
    IServiceCollection services)
{
    /// <summary>
    /// Gets the service collection used for dependency injection.
    /// </summary>
    public IServiceCollection Services { get; } = services;

    /// <summary>
    /// Adds and configures a document store with the specified name.
    /// </summary>
    /// <param name="storeName">The name of the document store.</param>
    /// <param name="builder">An action to configure the document store builder.</param>
    /// <returns>The current <see cref="ChroniclesBuilder"/> instance for chaining.</returns>
    public ChroniclesBuilder AddDocumentStore(
        string storeName,
        Action<DocumentStoreBuilder> builder)
    {
        builder.Invoke(new(storeName, Services));

        Services.AddSingleton<IDocumentStore>(s => new DocumentStore(
            storeName,
            s.GetRequiredService<IOptionsMonitor<DocumentOptions>>()));

        return this;
    }
}
