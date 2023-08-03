using Chronicles.Cosmos;
using Chronicles.Cosmos.Internal;

namespace Microsoft.Extensions.DependencyInjection;

public static class ServiceCollectionExtensions
{
    //public static IServiceCollection ConfigureChronicles(
    //    this IServiceCollection services,
    //    Func<IServiceProvider, ChroniclesCosmosOptions> optionsProvider)
    //    => services
    //        .AddSingleton(typeof(ICosmosReader<>), typeof(CosmosReader<>))
    //        .AddSingleton(typeof(ICosmosWriter<>), typeof(CosmosWriter<>))
    //        .AddSingleton<ICosmosContainerProvider, CosmosContainerProvider>()
    //        .AddSingleton<IJsonCosmosSerializer, JsonCosmosSerializer>()
    //        .AddSingleton<ICosmosClientProvider, CosmosClientProvider>()
    //        .AddSingleton<ICosmosLinqQuery, CosmosLinqQuery>()
    //        .AddSingleton<IOptions<ChroniclesCosmosOptions>>(s
    //            => new OptionsWrapper<ChroniclesCosmosOptions>(optionsProvider(s)));

    public static IServiceCollection ConfigureChroniclesContainer<T>(
        this IServiceCollection services,
        string name)
        where T : class
        => services
            .AddSingleton<ICosmosContainerNameProvider>(
                s => new CosmosContainerNameProvider<T>(name));

    public static IServiceCollection AddChronicles(
        this IServiceCollection services,
        Action<ChroniclesBuilder> builder)
    {
        builder.Invoke(new ChroniclesBuilder(services));
        return services;
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
        Action<ChroniclesCosmosOptions> optionsProvider)
    {
        services.Configure(optionsProvider);
        return this;
    }
}
