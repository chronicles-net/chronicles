using Microsoft.Azure.Cosmos;

namespace Chronicles.Documents.Internal;

public class SubscriptionInitializer : IContainerInitializer
{
    private readonly ThroughputProperties? throughput;

    public SubscriptionInitializer(
        ThroughputProperties? throughput = null)
        => this.throughput = throughput;

    public async Task InitializeAsync(
        Database database,
        ContainerProperties properties,
        CancellationToken cancellationToken = default)
    {
        properties.PartitionKeyPath = "/id";

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
