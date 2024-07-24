namespace Chronicles.EventStore.Internal;

internal interface IEventCatalogFactory
{
    IEventCatalog Get(
        string? storeName);
}