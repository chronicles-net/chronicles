using Microsoft.Extensions.DependencyInjection;

namespace Chronicles.EventStore.Internal.EventConsumers;

internal class EventConsumerFactory(
    IServiceProvider serviceProvider) : IEventConsumerFactory
{
    public IEventConsumer CreateConsumer(string name)
        => new CompositeEventConsumer(
            serviceProvider.GetKeyedServices<IEventConsumer>(name));
}