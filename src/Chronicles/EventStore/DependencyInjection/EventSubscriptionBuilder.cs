using Chronicles.EventStore;
using Chronicles.EventStore.Internal.EventConsumers;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Microsoft.Extensions.DependencyInjection;

public class EventSubscriptionBuilder
{
    private readonly string name;

    public EventSubscriptionBuilder(
        string name,
        IServiceCollection serviceCollection)
    {
        this.name = name;
        Services = serviceCollection;
    }

    public IServiceCollection Services { get; }

    public EventSubscriptionBuilder AddEventConsumer<T>()
        where T : class
    {
        Services.TryAddTransient<T>();
        Services.TryAddKeyedTransient<IEventConsumer, EventConsumer<T>>(name);

        return this;
    }

    public EventSubscriptionBuilder Configure(
        Action<EventSubscriptionOptions> configure)
    {
        Services.Configure(name, configure);

        return this;
    }
}
