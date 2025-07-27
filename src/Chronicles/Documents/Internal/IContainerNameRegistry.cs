namespace Chronicles.Documents.Internal;

/// <summary>
/// Represents a registry for document types to container name mappings.
/// </summary>
internal interface IContainerNameRegistry
{
    /// <summary>
    /// Gets the container name for the specified type.
    /// </summary>
    /// <typeparam name="T">The document type.</typeparam>
    /// <param name="storeName">(Optional) Name of the configured document store.</param>
    /// <returns>The container name and optional store name.</returns>
    string GetContainerName<T>(
        string? storeName = null);

    /// <summary>
    /// Gets the container name for the specified type.
    /// </summary>
    /// <param name="documentType">The document type.</param>
    /// <param name="storeName">(Optional) Name of the configured document store.</param>
    /// <returns>The container name and optional store name.</returns>
    string GetContainerName(
        Type documentType,
        string? storeName = null);
}
