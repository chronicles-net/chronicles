using Microsoft.Azure.Cosmos;

namespace Chronicles.Documents.Internal;

internal class DocumentInitializer : IContainerInitializer
{
    private readonly Action<ContainerProperties> containerProperties;
    private readonly ThroughputProperties? throughput;

    public DocumentInitializer(
        Type documentType,
        Action<ContainerProperties> containerProperties,
        ThroughputProperties? throughput = null)
    {
        DocumentType = documentType;
        this.containerProperties = containerProperties;
        this.throughput = throughput;
    }

    public Type DocumentType { get; }

    public async Task InitializeAsync(
        Database database,
        ContainerProperties properties,
        CancellationToken cancellationToken = default)
    {
        containerProperties.Invoke(properties);
        if (throughput == null)
        {
            await database.CreateContainerIfNotExistsAsync(
               properties,
               cancellationToken: cancellationToken);
        }
        else
        {
            await database.CreateContainerIfNotExistsAsync(
                properties,
                throughput,
                cancellationToken: cancellationToken);
        }
    }
}
