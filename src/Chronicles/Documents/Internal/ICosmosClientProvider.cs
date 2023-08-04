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
    /// <param name="clientName">(Optional) Name of the <see cref="CosmosClient"/>.</param>
    /// <returns>A <see cref="CosmosClient"/> instance.</returns>
    CosmosClient GetClient(string? clientName = null);

    /// <summary>
    /// Get the default <see cref="ICosmosSerializer"/> instance.
    /// </summary>
    /// <param name="clientName">(Optional) Name of the <see cref="CosmosClient"/>.</param>
    /// <returns>A <see cref="ICosmosSerializer"/> instance.</returns>
    ICosmosSerializer GetSerializer(string? clientName = null);
}
