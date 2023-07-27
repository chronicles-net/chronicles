using Microsoft.Azure.Cosmos;

namespace Chronicles.Cosmos.Internal;

/// <summary>
/// Represents a provider for cosmos <see cref="Container"/> instances.
/// </summary>
public interface ICosmosContainerProvider
{
    /// <summary>
    /// Get the configured container for the specified <see cref="ICosmosDocument"/> type.
    /// </summary>
    /// <typeparam name="T">The <see cref="ICosmosDocument"/>.</typeparam>
    /// Boolean indicating if the container should
    /// be configured for bulk operations. Default is false.
    /// <returns>A cosmos <see cref="Container"/>.</returns>
    Container GetContainer<T>();
}
