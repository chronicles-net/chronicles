namespace Chronicles.Documents.Internal;

/// <summary>
/// Represents a provider for cosmos <see cref="ICosmosSerializer"/> instances.
/// </summary>
public interface ICosmosSerializerProvider
{
    /// <summary>
    /// Get the configured container for the specified document type.
    /// </summary>
    /// <typeparam name="T">The <see cref="IDocument"/>.</typeparam>
    /// <returns>A cosmos <see cref="ICosmosSerializer"/>.</returns>
    ICosmosSerializer GetSerializer<T>();

    /// <summary>
    /// Get the configured container for the specified document type.
    /// </summary>
    /// <param name="documentType">
    /// The <see cref="Type"/> of the document.
    /// </param>
    /// <returns>A cosmos <see cref="ICosmosSerializer"/>.</returns>
    ICosmosSerializer GetSerializer(Type documentType);

    /// <summary>
    /// Get the container with a specified name.
    /// </summary>
    /// <param name="storeName">(Optional) Name of the configured document store.</param>
    /// <returns>A cosmos <see cref="ICosmosSerializer"/>.</returns>
    ICosmosSerializer GetSerializer(
        string? storeName = null);
}
