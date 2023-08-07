namespace Chronicles.Documents.Internal;

/// <summary>
/// Represents a registry for document types to container name mappings.
/// </summary>
public interface IContainerNameRegistry
{
    /// <summary>
    /// Add a mapping between a document type to a container name.
    /// </summary>
    /// <typeparam name="T">The document type</typeparam>
    /// <param name="containerName">The container name</param>
    /// <param name="storeName">(Optional) The configured document store.</param>
    void AddContainerName<T>(
        string containerName,
        string? storeName = null);

    /// <summary>
    /// Add a mapping between a document type to a container name.
    /// </summary>
    /// <param name="documentType">The document type</param>
    /// <param name="containerName">The container name</param>
    /// <param name="storeName">(Optional) The configured document store.</param>
    void AddContainerName(
        Type documentType,
        string containerName,
        string? storeName = null);

    /// <summary>
    /// Gets the container name for the specified type.
    /// </summary>
    /// <typeparam name="T">The document type.</typeparam>
    /// <returns>The container name and optional store name.</returns>
    ContainerNameAttribute GetContainerName<T>();

    /// <summary>
    /// Gets the container name for the specified type.
    /// </summary>
    /// <param name="documentType">The document type.</param>
    /// <returns>The container name and optional store name.</returns>
    ContainerNameAttribute GetContainerName(
        Type documentType);
}