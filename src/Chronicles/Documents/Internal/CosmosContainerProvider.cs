using System.Collections.Concurrent;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Options;

namespace Chronicles.Documents.Internal;

public class CosmosContainerProvider : ICosmosContainerProvider
{
    private readonly ConcurrentDictionary<DocumentTypeKey, Container> containers = new();
    private readonly ICosmosClientProvider clientProvider;
    private readonly IContainerNameRegistry registry;
    private readonly IOptionsMonitor<DocumentOptions> options;

    public CosmosContainerProvider(
        ICosmosClientProvider clientProvider,
        IContainerNameRegistry registry,
        IOptionsMonitor<DocumentOptions> options)
    {
        this.clientProvider = clientProvider;
        this.registry = registry;
        this.options = options;
    }

    public Container GetContainer<T>(
        string? storeName = null)
        => GetContainer(typeof(T), storeName);

    public Container GetContainer(
        Type documentType,
        string? storeName = null)
    {
        var key = new DocumentTypeKey(documentType, storeName ?? string.Empty);
        if (containers.TryGetValue(key, out var container))
        {
            return container;
        }

        var name = registry.GetContainerName(documentType);
        return containers
            .GetOrAdd(
                key,
                t => GetContainer(name, storeName));
    }

    public Container GetContainer(
        string containerName,
        string? storeName = null)
        => clientProvider
            .GetClient(storeName)
            .GetContainer(
                options.Get(storeName).DatabaseName,
                containerName);

    public Container GetSubscriptionContainer(
        string? storeName = null)
        => GetContainer(
            options.Get(storeName).SubscriptionContainerName,
            storeName);

    public ICosmosSerializer GetSerializer(
        string? storeName = default)
        => clientProvider.GetSerializer(storeName);
}
