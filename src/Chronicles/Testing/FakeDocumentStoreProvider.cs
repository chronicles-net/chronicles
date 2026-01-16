using System.Collections.Immutable;
using System.Text.Json;
using Chronicles.Documents;
using Chronicles.Documents.Internal;

namespace Chronicles.Testing;

public interface IFakeDocumentStoreProvider
{
    FakeDocumentStore GetStore(string? storeName);
}

internal class FakeDocumentStoreProvider
    : IFakeDocumentStoreProvider
{
    private readonly ImmutableDictionary<string, FakeDocumentStore> stores;

    public FakeDocumentStoreProvider(
        IContainerNameRegistry registry,
        IEnumerable<IDocumentStore> stores)
    {
        var docStores = new Dictionary<string, IDocumentStore>();
        foreach (var store in stores)
        {
            docStores[store.Name] = store;
        }

        this.stores = docStores.ToImmutableDictionary(
            docStore => docStore.Key,
            docStore => new FakeDocumentStore(registry, docStore.Value));
    }

    protected FakeDocumentStoreProvider(
        JsonSerializerOptions options)
    {
        stores =
            new[] { FakeDocumentStore.FromOptions(DocumentOptions.DefaultStoreName, "default", options) }
            .ToImmutableDictionary(
                docStore => docStore.Name,
                docStore => docStore);
    }

    public FakeDocumentStore GetStore(
        string? storeName)
        => stores.TryGetValue(storeName ?? DocumentOptions.DefaultStoreName, out var store)
         ? store
         : throw new KeyNotFoundException($"Store '{storeName}' not found.");

    public static FakeDocumentStoreProvider FromOptions(
        JsonSerializerOptions options)
        => new FakeDocumentStoreProvider(options);
}
