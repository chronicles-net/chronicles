using Microsoft.Azure.Cosmos;

namespace Chronicles.Documents.Internal;

/// <summary>
/// Represents a provider for configured
/// <see cref="CosmosClient"/> instances.
/// </summary>
public interface ICosmosClientProvider
{
    /// <summary>
    /// Get the default <see cref="CosmosClient"/> instance.
    /// </summary>
    /// <returns>A <see cref="CosmosClient"/> instance.</returns>
    CosmosClient GetClient();
}
