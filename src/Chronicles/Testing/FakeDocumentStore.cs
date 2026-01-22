using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Text.Json;
using Chronicles.Documents.Internal;

namespace Chronicles.Testing;

public class FakeDocumentStore
{
    private readonly ConcurrentDictionary<string, FakeContainer> containers;
    private readonly IContainerNameRegistry registry;

    internal FakeDocumentStore(
        IContainerNameRegistry registry,
        IDocumentStore store)
    {
        this.registry = registry;
        Name = store.Name;
        SerializerOptions = store.Options.SerializerOptions;
        containers = new();
    }

    private FakeDocumentStore(
        IContainerNameRegistry registry,
        string storeName,
        string containerName,
        JsonSerializerOptions serializerOptions)
    {
        this.registry = registry;
        containers = new() { [containerName] = new FakeContainer(containerName) };
        Name = storeName;
        SerializerOptions = serializerOptions;
    }

    public ImmutableList<FakeContainer> Containers => [.. containers.Values];

    public string Name { get; }

    public JsonSerializerOptions SerializerOptions { get; }

    public FakeContainer GetContainer<T>()
        => containers.GetOrAdd(
            registry.GetContainerName<T>(Name),
            cn => new FakeContainer(cn));

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
