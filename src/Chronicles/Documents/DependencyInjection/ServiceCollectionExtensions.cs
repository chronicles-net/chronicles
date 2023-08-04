using Chronicles.Documents;
using Chronicles.Documents.Internal;

namespace Microsoft.Extensions.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddChronicles(
        this IServiceCollection services,
        Action<ChroniclesBuilder> builder)
    {
        builder.Invoke(new ChroniclesBuilder(services));

        return services
            .AddSingleton(typeof(IDocumentReader<>), typeof(CosmosReader<>))
            .AddSingleton(typeof(IDocumentWriter<>), typeof(CosmosWriter<>))
            .AddSingleton<ICosmosClientProvider, CosmosClientProvider>()
            .AddSingleton<ICosmosSerializerProvider, CosmosSerializerProvider>()
            .AddSingleton<ICosmosContainerProvider, CosmosContainerProvider>()
            .AddSingleton<ICosmosLinqQuery, CosmosLinqQuery>();
    }

    public static IServiceCollection AddChronicles(
        this IServiceCollection services)
        => AddChronicles(services, builder => { });
}

public class ChroniclesBuilder
{
    private readonly IServiceCollection services;

    public ChroniclesBuilder(
        IServiceCollection services)
    {
        this.services = services;
    }

    public ChroniclesBuilder WithOptions(
        Action<DocumentOptions> optionsProvider)
    {
        services.Configure(optionsProvider);
        return this;
    }

    public ChroniclesBuilder WithOptions(
        string name,
        Action<DocumentOptions> optionsProvider)
    {
        services.Configure(name, optionsProvider);
        return this;
    }
}
