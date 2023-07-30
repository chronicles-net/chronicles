using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Options;

namespace Chronicles.Cosmos.Internal;

public class CosmosContainerProvider : ICosmosContainerProvider
{
    private readonly IEnumerable<ICosmosContainerNameProvider> nameProviders;
    private readonly ICosmosClientProvider clientProvider;
    private readonly IOptions<ChroniclesCosmosOptions> options;

    public CosmosContainerProvider(
        IEnumerable<ICosmosContainerNameProvider> nameProviders,
        ICosmosClientProvider clientProvider,
        IOptions<ChroniclesCosmosOptions> options)
    {
        this.nameProviders = nameProviders;
        this.clientProvider = clientProvider;
        this.options = options;
    }

    public Container GetContainer<T>()
        => GetContainer(
            GetContainerName(typeof(T)));

    public Container GetContainer(
        Type resourceType)
        => GetContainer(
            GetContainerName(resourceType));

    public Container GetContainer(
        string name)
        => clientProvider
            .GetClient()
            .GetContainer(
                options.Value.DatabaseName,
                name);

    private string GetContainerName(
        Type resourceType)
    {
        foreach (var provider in nameProviders)
        {
            if (provider.GetContainerName(resourceType) is { } name)
            {
                return name;
            }
        }

        throw new NotSupportedException(
            $"Type {resourceType.Name} is not supported.");
    }
}
