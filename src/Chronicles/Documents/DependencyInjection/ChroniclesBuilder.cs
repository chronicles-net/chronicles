using Chronicles.Documents.Internal;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Chronicles.Documents.DependencyInjection;

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
