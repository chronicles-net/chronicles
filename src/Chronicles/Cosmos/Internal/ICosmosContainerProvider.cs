using Microsoft.Azure.Cosmos;

namespace Chronicles.Cosmos.Internal;

/// <summary>
/// Represents a provider for cosmos <see cref="Container"/> instances.
/// </summary>
public interface ICosmosContainerProvider
{
    /// <summary>
    /// Get the configured container for the specified document type.
    /// </summary>
    /// <typeparam name="T">The <see cref="ICosmosDocument"/>.</typeparam>
    /// Boolean indicating if the container should
    /// be configured for bulk operations. Default is false.
    /// <returns>A cosmos <see cref="Container"/>.</returns>
    Container GetContainer<T>();

    /// <summary>
    /// Get the configured container for the specified document type.
    /// </summary>
    /// <param name="resourceType">
    /// The <see cref="Type"/> of the document.
    /// </param>
    /// <returns>A cosmos <see cref="Container"/>.</returns>
    Container GetContainer(Type resourceType);

    /// <summary>
    /// Get the container with a specified name.
    /// </summary>
    /// <param name="containerName">The name of the container.</param>
    /// <param name="databaseName">(Optional) The name of the database.</param>
    /// <returns>A cosmos <see cref="Container"/>.</returns>
    Container GetContainer(
        string containerName,
        string? databaseName = null);
}
