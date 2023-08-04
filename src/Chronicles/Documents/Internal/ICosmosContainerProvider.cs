using Microsoft.Azure.Cosmos;

namespace Chronicles.Documents.Internal;

/// <summary>
/// Represents a provider for cosmos <see cref="Container"/> instances.
/// </summary>
public interface ICosmosContainerProvider
{
    /// <summary>
    /// Get the configured container for the specified document type.
    /// </summary>
    /// <typeparam name="T">The <see cref="IDocument"/>.</typeparam>
    /// <returns>A cosmos <see cref="Container"/>.</returns>
    Container GetContainer<T>();

    /// <summary>
    /// Get the configured container for the specified document type.
    /// </summary>
    /// <param name="documentType">
    /// The <see cref="Type"/> of the document.
    /// </param>
    /// <returns>A cosmos <see cref="Container"/>.</returns>
    Container GetContainer(Type documentType);

    /// <summary>
    /// Get the container with a specified name.
    /// </summary>
    /// <param name="containerName">The name of the container.</param>
    /// <param name="clientName">(Optional) Name of the <see cref="CosmosClient"/>.</param>
    /// <returns>A cosmos <see cref="Container"/>.</returns>
    Container GetContainer(
        string containerName,
        string? clientName = null);

    Container GetSubscriptionContainer<T>();
}
