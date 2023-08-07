using Microsoft.Azure.Cosmos;

namespace Chronicles.Documents.Internal;

public class ContainerInitializer : IContainerInitializer
{
    private readonly ContainerProperties properties;
    private readonly ThroughputProperties? throughput;

    public ContainerInitializer(
        ContainerProperties properties,
        ThroughputProperties? throughput = null)
    {
        this.properties = properties;
        this.throughput = throughput;
    }

    public Task InitializeAsync(
        Database database,
        CancellationToken cancellationToken)
        => throughput == null
         ? database.CreateContainerIfNotExistsAsync(
            properties,
            cancellationToken: cancellationToken)
         : database.CreateContainerIfNotExistsAsync(
            properties,
            throughput,
            cancellationToken: cancellationToken);
}
