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

    public InitializationOptions CreateContainer(
        ContainerProperties properties,
        ThroughputProperties? throughput = null)
        => CreateContainer(
            new ContainerInitializer(
                properties,
                throughput));

    public InitializationOptions CreateContainer(
        IContainerInitializer initializer)
    {
        initializers.Add(initializer);

        return this;
    }
}
