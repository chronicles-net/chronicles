using System.Collections.Immutable;
using System.Text.Json;
using Chronicles.Documents.Internal;

namespace Chronicles.Testing;

public class FakeDocumentStore
{
    private readonly ImmutableDictionary<string, FakeContainer> containers;
    private readonly IContainerNameRegistry registry;

    public FakeDocumentStore(
        IContainerNameRegistry registry,
        IDocumentStore store)
    {
        containers = store.Options
            .Initialization
            .Containers
            .OfType<DocumentInitializer>()
            .Select(initializer => new FakeContainer(
                registry.GetContainerName(
                    initializer.DocumentType,
                    store.Name)))
            .ToImmutableDictionary(
                container => container.Name,
                container => container);
        this.registry = registry;
        Name = store.Name;
        SerializerOptions = store.Options.SerializerOptions;
    }

    protected FakeDocumentStore(
        IContainerNameRegistry registry,
        string storeName,
        string containerName,
        JsonSerializerOptions serializerOptions)
    {
        this.registry = registry;
        containers = new[] { new FakeContainer(containerName) }
            .ToImmutableDictionary(
                container => container.Name,
                container => container);
        Name = storeName;
        SerializerOptions = serializerOptions;
    }

    public ImmutableList<FakeContainer> Containers => [.. containers.Values];

    public string Name { get; }

    public JsonSerializerOptions SerializerOptions { get; }

    public FakeContainer GetContainer<T>()
        => containers.TryGetValue(
            registry.GetContainerName<T>(Name),
            out var container)
         ? container
         : throw new KeyNotFoundException(
             $"Container for type '{typeof(T).Name}' not found in store '{Name}'.");

    public static FakeDocumentStore FromOptions(
        string storeName,
        string containerName,
        JsonSerializerOptions serializerOptions)
        => new FakeDocumentStore(
            new FakeContainerNameRegistry(containerName),
            storeName,
            containerName,
            serializerOptions);
}
