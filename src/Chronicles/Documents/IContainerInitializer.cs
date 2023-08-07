using Microsoft.Azure.Cosmos;

namespace Chronicles.Documents;

/// <summary>
/// Represents an initializer for a Cosmos container.
/// </summary>
public interface IContainerInitializer
{
    /// <summary>
    /// Initializes the container in Cosmos.
    /// </summary>
    /// <param name="database">The Cosmos database.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> used.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    Task InitializeAsync(
        Database database,
        CancellationToken cancellationToken);
}
