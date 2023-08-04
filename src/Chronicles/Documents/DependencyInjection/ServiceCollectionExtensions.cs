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
            .AddSingleton<ICosmosLinqQuery, CosmosLinqQuery>();

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
        string name,
        Action<DocumentOptions> optionsProvider)
    {
        services.Configure(name, optionsProvider);
        return this;
    }

    public ChroniclesBuilder AddDocumentStore(
        string name)
        => AddDocumentStore(name, options => { });
}
