using Chronicles.EventStore.Internal;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Chronicles.EventStore.DependencyInjection;

public class EventSubscriptionBuilder(
    string name,
    string? storeName,
    IServiceCollection serviceCollection)
{
    public IServiceCollection Services { get; } = serviceCollection;

    public EventSubscriptionBuilder AddExceptionHandler<TExceptionHandler>()
        where TExceptionHandler : class, IEventSubscriptionExceptionHandler
    {
        Services.TryAddKeyedSingleton<IEventSubscriptionExceptionHandler, TExceptionHandler>(name);

        return this;
    }

    public EventSubscriptionBuilder MapStream(
        string streamCategory,
        Action<EventProcessorBuilder> consumerBuilder)
    {
        var key = $"{name}:{streamCategory}";

        consumerBuilder.Invoke(new EventProcessorBuilder(key, storeName, Services));

        Services.AddKeyedSingleton<IEventStreamProcessor>(name, (s, n) =>
            new EventStreamProcessor(
                streamCategory,
                s.GetKeyedServices<IEventProcessor>(key)));

        return this;
    }

    public EventSubscriptionBuilder MapAllStreams(
        Action<EventProcessorBuilder> consumerBuilder)
    {
        var key = $"{name}:__all__";

        consumerBuilder.Invoke(new EventProcessorBuilder(key, storeName, Services));
        Services.AddKeyedSingleton<IEventStreamProcessor>(name, (s, n) =>
            new EventStreamProcessor(
                categoryName: null,
                s.GetKeyedServices<IEventProcessor>(key)));

        return this;
    }
}
