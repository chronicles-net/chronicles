using Microsoft.Extensions.DependencyInjection;

namespace Chronicles.EventStore.DependencyInjection;

public class EventProcessorBuilder(
    string name,
    string? storeName,
    IServiceCollection serviceCollection)
{
    public string? StoreName { get; } = storeName;

    public string Name { get; } = name;

    public IServiceCollection Services { get; } = serviceCollection;

    public EventProcessorBuilder AddEventProcessor<TProcessor>()
        where TProcessor : class, IEventProcessor
    {
        Services.AddKeyedSingleton<IEventProcessor, TProcessor>(Name);

        return this;
    }
}
