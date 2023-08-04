using System.Collections.Concurrent;
using System.Reflection;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Options;

namespace Chronicles.Cosmos.Internal;

public class CosmosContainerProvider : ICosmosContainerProvider
{
    private static readonly ConcurrentDictionary<Type, Container> Containers = new();
    private readonly ICosmosClientProvider clientProvider;
    private readonly IOptions<ChroniclesCosmosOptions> options;

    public CosmosContainerProvider(
        ICosmosClientProvider clientProvider,
        IOptions<ChroniclesCosmosOptions> options)
    {
        this.clientProvider = clientProvider;
        this.options = options;
    }

    public Container GetContainer<T>()
        => GetContainer(typeof(T));

    public Container GetContainer(
        Type resourceType)
    {
        if (Containers.TryGetValue(resourceType, out var container))
        {
            return container;
        }

        if (resourceType.GetCustomAttribute<ContainerNameAttribute>(inherit: true) is not { } a)
        {
            throw new ArgumentException(
                $"Type {resourceType.Name} is not supported. " +
                $"Missing {nameof(ContainerNameAttribute)}.",
                nameof(resourceType));
        }

        container = GetContainer(a.ContainerName, a.DatabaseName);
        Containers[resourceType] = container;

        return container;
    }

    public Container GetContainer(
        string containerName,
        string? databaseName = default)
        => clientProvider
            .GetClient()
            .GetContainer(
                databaseName ?? options.Value.DefaultDatabaseName,
                containerName);
}
