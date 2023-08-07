using System.Collections.Concurrent;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Options;

namespace Chronicles.Documents.Internal;

public class CosmosContainerProvider : ICosmosContainerProvider
{
    private readonly ConcurrentDictionary<Type, Container> containers = new();
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

    public Container GetContainer<T>()
        => GetContainer(typeof(T));

    public Container GetContainer(
        Type documentType)
    {
        if (containers.TryGetValue(documentType, out var container))
        {
            return container;
        }

        var name = registry.GetContainerName(documentType);
        return containers
            .GetOrAdd(
                documentType,
                t => GetContainer(name.ContainerName, name.StoreName));
    }

    public Container GetContainer(
        string containerName,
        string? storeName = null)
        => clientProvider
            .GetClient(storeName)
            .GetContainer(
                options.Get(storeName).DatabaseName,
                containerName);

    public Container GetSubscriptionContainer<T>()
        => GetSubscriptionContainer(typeof(T));

    public Container GetSubscriptionContainer(Type documentType)
        => GetSubscriptionContainer(
            registry
                .GetContainerName(documentType).StoreName);

    public Container GetSubscriptionContainer(
        string? storeName = null)
        => GetContainer(
            options.Get(storeName).SubscriptionContainerName,
            storeName);
}
