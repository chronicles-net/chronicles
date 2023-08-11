using Chronicles.Documents.Internal;
using Microsoft.Azure.Cosmos;

namespace Chronicles.Documents;

public class InitializationOptions
{
    private readonly List<IContainerInitializer> initializers = new();

    public ThroughputProperties? Database { get; private set; }

    public IReadOnlyList<IContainerInitializer> Containers => initializers;

    public InitializationOptions CreateSubscriptionContainer(
        ThroughputProperties? throughput = null)
    {
        if (initializers.OfType<SubscriptionInitializer>().Any())
        {
            return this;
        }

        return CreateContainer(new SubscriptionInitializer(throughput));
    }

    public InitializationOptions CreateDatabase(
        ThroughputProperties? throughput = null)
    {
        Database = throughput
            ?? ThroughputProperties.CreateManualThroughput(400);

        return this;
    }

    public InitializationOptions CreateContainer<T>(
        Action<ContainerProperties>? containerProperties = null,
        ThroughputProperties? throughputProperties = null)
        => CreateContainer(
            typeof(T),
            containerProperties ?? (_ => { }),
            throughputProperties);

    public InitializationOptions CreateContainer(
        Type documentType,
        Action<ContainerProperties> containerProperties,
        ThroughputProperties? throughput = null)
        => CreateContainer(
            new DocumentInitializer(
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
