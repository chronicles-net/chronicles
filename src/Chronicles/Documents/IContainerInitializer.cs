using Microsoft.Azure.Cosmos;

namespace Chronicles.Documents;

/// <summary>
/// Defines a contract for initializing Cosmos DB containers in Chronicles.
/// Implement this interface to provide custom logic for creating and configuring containers, such as setting partition keys, throughput, or indexing policies.
/// Use when you need to automate or customize the setup of containers for document storage, event sourcing, or subscriptions using cosmos emulator.
/// </summary>
public interface IContainerInitializer
{
    /// <summary>
    /// Initializes a Cosmos DB container with the specified properties in the given database.
    /// Implement this method to create the container, set its configuration, and handle any initialization logic required for your application.
    /// </summary>
    /// <param name="database">The Cosmos DB database where the container will be created.</param>
    /// <param name="properties">The properties and configuration for the container.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    Task InitializeAsync(
        Database database,
        ContainerProperties properties,
        CancellationToken cancellationToken = default);
}
