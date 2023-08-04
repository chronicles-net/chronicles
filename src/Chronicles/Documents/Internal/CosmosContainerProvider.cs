using System.Collections.Concurrent;
using System.Reflection;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Options;

namespace Chronicles.Documents.Internal;

public class CosmosContainerProvider : ICosmosContainerProvider
{
    private readonly ConcurrentDictionary<Type, Container> containers = new();
    private readonly ICosmosClientProvider clientProvider;
    private readonly IOptionsMonitor<DocumentOptions> options;

    public CosmosContainerProvider(
        ICosmosClientProvider clientProvider,
        IOptionsMonitor<DocumentOptions> options)
    {
        this.clientProvider = clientProvider;
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

        if (documentType.GetCustomAttribute<ContainerNameAttribute>(inherit: true) is not { } a)
        {
            throw new ArgumentException(
                $"Type {documentType.Name} is not supported. " +
                $"Missing {nameof(ContainerNameAttribute)}.",
                nameof(documentType));
        }

        return containers
            .GetOrAdd(
                documentType,
                t => GetContainer(a.ContainerName, a.ClientName));
    }

    public Container GetContainer(
        string containerName,
        string? clientName = default)
        => clientProvider
            .GetClient(clientName)
            .GetContainer(
                options.Get(clientName).DatabaseName,
                containerName);
}
