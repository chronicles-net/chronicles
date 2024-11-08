using Microsoft.Extensions.DependencyInjection;

namespace Chronicles.EventStore.Internal;

/// <summary>
/// Represents a factory for creating event catalogs.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="EventCatalogFactory"/> class.
/// </remarks>
/// <param name="serviceProvider">The service provider.</param>
internal class EventCatalogFactory(
    IServiceProvider serviceProvider)
    : IEventCatalogFactory
{
    /// <summary>
    /// Gets the event catalog for the specified store name.
    /// </summary>
    /// <param name="storeName">The name of the store.</param>
    /// <returns>The event catalog.</returns>
    public IEventCatalog Get(
        string? storeName)
        => serviceProvider
            .GetRequiredKeyedService<IEventCatalog>(
                storeName ?? string.Empty);
}
