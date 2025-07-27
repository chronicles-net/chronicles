using Microsoft.Azure.Cosmos;

namespace Chronicles.Documents.Internal;

/// <summary>
/// Represents a provider for configured
/// <see cref="CosmosClient"/> instances.
/// </summary>
internal interface ICosmosClientProvider
{
    /// <summary>
    /// Get the default <see cref="CosmosClient"/> instance.
    /// </summary>
    /// <param name="storeName">(Optional) Name of the configured document store.</param>
    /// <returns>A <see cref="CosmosClient"/> instance.</returns>
    CosmosClient GetClient(string? storeName = null);
}
