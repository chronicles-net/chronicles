using Microsoft.Azure.Cosmos;

namespace Chronicles.Documents;

public interface IContainerInitializer
{
    Type DocumentType { get; }

    Task InitializeAsync(
        Database database,
        ContainerProperties properties,
        CancellationToken cancellationToken = default);
}