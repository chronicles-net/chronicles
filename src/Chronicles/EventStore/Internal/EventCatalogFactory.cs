using Microsoft.Extensions.DependencyInjection;

namespace Chronicles.EventStore.Internal;

internal class EventCatalogFactory(
    IServiceProvider serviceProvider) : IEventCatalogFactory
{
    public IEventCatalog Get(string? storeName)
        => serviceProvider.GetRequiredKeyedService<IEventCatalog>(storeName ?? string.Empty);
}