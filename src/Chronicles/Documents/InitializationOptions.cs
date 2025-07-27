using Chronicles.Documents.Internal;
using Microsoft.Azure.Cosmos;

namespace Chronicles.Documents;

/// <summary>
/// Provides options and methods for configuring database and container initialization
/// in Chronicles using Cosmos DB.
/// Use this class to define how databases and containers are created, set throughput,
/// and register custom initializers for document and subscription containers.
/// </summary>
public class InitializationOptions
{
    private readonly List<IContainerInitializer> initializers = [];

    /// <summary>
    /// Gets the throughput properties for the database, if configured.
    /// </summary>
    public ThroughputProperties? Database { get; private set; }

    /// <summary>
    /// Gets the list of container initializers to be executed during initialization.
    /// </summary>
    public IReadOnlyList<IContainerInitializer> Containers => initializers;

    /// <summary>
    /// Registers a subscription container initializer, optionally configuring throughput.
    /// Use this method to ensure a container for subscriptions is created during initialization.
    /// </summary>
    /// <param name="throughput">Optional throughput settings for the subscription container.</param>
    /// <returns>The updated <see cref="InitializationOptions"/> instance for chaining.</returns>
    public InitializationOptions CreateSubscriptionContainer(
        ThroughputProperties? throughput = null)
    {
        if (initializers.OfType<SubscriptionInitializer>().Any())
        {
            return this;
        }

        return CreateContainer(new SubscriptionInitializer(throughput));
    }

    /// <summary>
    /// Configures the database to be created with the specified throughput.
    /// Use this method to set the throughput for the Cosmos DB database during initialization.
    /// </summary>
    /// <param name="throughput">Optional throughput settings for the database. Defaults to 400 RU if not specified.</param>
    /// <returns>The updated <see cref="InitializationOptions"/> instance for chaining.</returns>
    public InitializationOptions CreateDatabase(
        ThroughputProperties? throughput = null)
    {
        Database = throughput
            ?? ThroughputProperties.CreateManualThroughput(400);

        return this;
    }

    /// <summary>
    /// Registers a document container initializer for the specified document type, with optional container and throughput configuration.
    /// Use this method to ensure a container for the document type is created during initialization.
    /// </summary>
    /// <typeparam name="T">The type of document for which to create the container.</typeparam>
    /// <param name="containerProperties">Optional action to configure <see cref="ContainerProperties"/> for the container.</param>
    /// <param name="throughputProperties">Optional throughput settings for the container.</param>
    /// <returns>The updated <see cref="InitializationOptions"/> instance for chaining.</returns>
    public InitializationOptions CreateContainer<T>(
        Action<ContainerProperties>? containerProperties = null,
        ThroughputProperties? throughputProperties = null)
        => CreateContainer(
            typeof(T),
            containerProperties ?? (_ => { }),
            throughputProperties);

    /// <summary>
    /// Registers a document container initializer for the specified document type, with custom container configuration and throughput.
    /// Use this method to ensure a container for the document type is created during initialization.
    /// </summary>
    /// <param name="documentType">The type of document for which to create the container.</param>
    /// <param name="containerProperties">Action to configure <see cref="ContainerProperties"/> for the container.</param>
    /// <param name="throughput">Optional throughput settings for the container.</param>
    /// <returns>The updated <see cref="InitializationOptions"/> instance for chaining.</returns>
    public InitializationOptions CreateContainer(
        Type documentType,
        Action<ContainerProperties> containerProperties,
        ThroughputProperties? throughput = null)
        => CreateContainer(
            new DocumentInitializer(
                documentType,
                containerProperties,
                throughput));

    /// <summary>
    /// Registers a custom container initializer to be executed during initialization.
    /// Use this method to add any custom logic for container creation, such as for special types or advanced scenarios.
    /// </summary>
    /// <param name="initializer">The container initializer to register.</param>
    /// <returns>The updated <see cref="InitializationOptions"/> instance for chaining.</returns>
    public InitializationOptions CreateContainer(
        IContainerInitializer initializer)
    {
        initializers.Add(initializer);

        return this;
    }
}
