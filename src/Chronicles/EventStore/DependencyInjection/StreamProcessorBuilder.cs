using Chronicles.EventStore;

namespace Microsoft.Extensions.DependencyInjection;

public class StreamProcessorBuilder(
    string key,
    IServiceCollection serviceCollection)
{
    private readonly string key = key;

    public IServiceCollection Services { get; } = serviceCollection;

    public StreamProcessorBuilder AddEventProcessor<TProcessor>()
        where TProcessor : class, IEventProcessor
    {
        Services.AddKeyedSingleton<IEventProcessor, TProcessor>(key);

        return this;
    }
}
