using Chronicles.Documents;
using Chronicles.Documents.Internal;
using Chronicles.Documents.Serialization;

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
            .AddSingleton<ICosmosContainerProvider, CosmosContainerProvider>()
            .AddSingleton<IJsonCosmosSerializer, JsonCosmosSerializer>()
            .AddSingleton<ICosmosClientProvider, CosmosClientProvider>()
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
}
