using Microsoft.Azure.Cosmos;

namespace Chronicles.Documents.Internal;

/// <summary>
/// Represents a provider for cosmos <see cref="Container"/> instances.
/// </summary>
internal interface ICosmosContainerProvider
{
    /// <summary>
    /// Gets the configured container for the specified document type.
    /// </summary>
    /// <typeparam name="T">The document type.</typeparam>
    /// <param name="storeName">(Optional) Name of the configured document store.</param>
    /// <returns>A cosmos <see cref="Container"/>.</returns>
    Container GetContainer<T>(string? storeName = null);

    /// <summary>
    /// Gets the configured container for the specified document type.
    /// </summary>
    /// <param name="documentType">
    /// The <see cref="Type"/> of the document.
    /// </param>
    /// <param name="storeName">(Optional) Name of the configured document store.</param>
    /// <returns>A cosmos <see cref="Container"/>.</returns>
    Container GetContainer(Type documentType, string? storeName = null);

    /// <summary>
    /// Gets the container with a specified name.
    /// </summary>
    /// <param name="containerName">The name of the container.</param>
    /// <param name="storeName">(Optional) Name of the configured document store.</param>
    /// <returns>A cosmos <see cref="Container"/>.</returns>
    Container GetContainer(
        string containerName,
        string? storeName = null);

    /// <summary>
    /// Gets the container containing the subscription leases for the specified store name.
    /// </summary>
    /// <param name="storeName">(Optional) Name of the configured document store.</param>
    /// <returns>A cosmos <see cref="Container"/>.</returns>
    Container GetSubscriptionContainer(
        string? storeName = null);
}
