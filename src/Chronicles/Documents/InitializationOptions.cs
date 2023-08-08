using Chronicles.Documents.Internal;
using Microsoft.Azure.Cosmos;

namespace Chronicles.Documents;

public class InitializationOptions
{
    private readonly List<IContainerInitializer> initializers = new();

    public ThroughputProperties? Database { get; private set; }

    public IReadOnlyList<IContainerInitializer> Containers => initializers;

    public InitializationOptions CreateDatabase(ThroughputProperties throughput)
    {
        Database = throughput;
        return this;
    }

    public InitializationOptions CreateContainer<T>(
        Action<ContainerProperties> containerProperties,
        ThroughputProperties? throughputProperties = null)
        => CreateContainer(
            typeof(T),
            containerProperties,
            throughputProperties);

    public InitializationOptions CreateContainer(
        Type documentType,
        Action<ContainerProperties> containerProperties,
        ThroughputProperties? throughput = null)
        => CreateContainer(
            new ContainerInitializer(
                documentType,
                containerProperties,
                throughput));

    public InitializationOptions CreateContainer(
        IContainerInitializer initializer)
    {
        initializers.Add(initializer);

        return this;
    }
}
