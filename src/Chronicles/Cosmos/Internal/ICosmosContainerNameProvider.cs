namespace Chronicles.Cosmos.Internal;

/// <summary>
/// Represents a provider for container names.
/// </summary>
public interface ICosmosContainerNameProvider
{
    /// <summary>
    /// Resolves the configured container name for
    /// the specified document type.
    /// </summary>
    /// <param name="resourceType">The <see cref="Type"/>
    /// of the document stored in Cosmos.</param>
    /// <returns>A container name.</returns>
    string? GetContainerName(
        Type resourceType);
}
