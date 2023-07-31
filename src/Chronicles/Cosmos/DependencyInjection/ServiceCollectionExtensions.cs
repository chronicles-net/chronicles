using Chronicles.Cosmos;
using Chronicles.Cosmos.Internal;
using Chronicles.Cosmos.Serialization;
using Microsoft.Extensions.Options;

namespace Microsoft.Extensions.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection ConfigureChronicles(
        this IServiceCollection services,
        Func<IServiceProvider, ChroniclesCosmosOptions> optionsProvider)
        => services
            .AddSingleton(typeof(ICosmosReader<>), typeof(CosmosReader<>))
            .AddSingleton(typeof(ICosmosWriter<>), typeof(CosmosWriter<>))
            .AddSingleton<ICosmosContainerProvider, CosmosContainerProvider>()
            .AddSingleton<IJsonCosmosSerializer, JsonCosmosSerializer>()
            .AddSingleton<ICosmosClientProvider, CosmosClientProvider>()
            .AddSingleton<ICosmosLinqQuery, CosmosLinqQuery>()
            .AddSingleton<IOptions<ChroniclesCosmosOptions>>(s
                => new OptionsWrapper<ChroniclesCosmosOptions>(optionsProvider(s)));

    public static IServiceCollection ConfigureChroniclesContainer<T>(
        this IServiceCollection services,
        string name)
        where T : class
        => services
            .AddSingleton<ICosmosContainerNameProvider>(
                s => new CosmosContainerNameProvider<T>(name));
}
