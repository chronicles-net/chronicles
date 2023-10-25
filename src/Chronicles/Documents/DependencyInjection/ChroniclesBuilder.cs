using Chronicles.Documents;
using Chronicles.Documents.Internal;
using Microsoft.Extensions.Options;

namespace Microsoft.Extensions.DependencyInjection;

public class ChroniclesBuilder(
    IServiceCollection services)
{
    public IServiceCollection Services { get; } = services;

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
