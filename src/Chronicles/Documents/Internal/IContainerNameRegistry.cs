namespace Chronicles.Documents.Internal;

/// <summary>
/// Represents a registry for document types to container name mappings.
/// </summary>
public interface IContainerNameRegistry
{
    /// <summary>
    /// Gets the container name for the specified type.
    /// </summary>
    /// <typeparam name="T">The document type.</typeparam>
    /// <returns>The container name and optional store name.</returns>
    DocumentContainer GetContainerName<T>();

    /// <summary>
    /// Gets the container name for the specified type.
    /// </summary>
    /// <param name="documentType">The document type.</param>
    /// <returns>The container name and optional store name.</returns>
    DocumentContainer GetContainerName(
        Type documentType);
}