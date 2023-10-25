using Chronicles.EventStore;

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
        Services.AddSingleton<T>();

        return this;
    }

    public EventSubscriptionBuilder Configure(
        Action<EventSubscriptionOptions> configure)
    {
        Services.Configure(name, configure);

        return this;
    }
}
