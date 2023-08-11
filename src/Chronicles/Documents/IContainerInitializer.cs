using Microsoft.Azure.Cosmos;

namespace Chronicles.Documents;

public interface IContainerInitializer
{
    Task InitializeAsync(
        Database database,
        ContainerProperties properties,
        CancellationToken cancellationToken = default);
}