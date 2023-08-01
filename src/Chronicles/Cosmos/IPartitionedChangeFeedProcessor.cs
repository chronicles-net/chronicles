namespace Chronicles.Cosmos;

/// <summary>
/// Represents a processor for a Cosmos change feed.
/// </summary>
/// <typeparam name="T">
/// The type of <see cref="ICosmosDocument"/>
/// to be processed.
/// </typeparam>
public interface IPartitionedChangeFeedProcessor<in T>
    where T : class, ICosmosDocument
{
    /// <summary>
    /// Processes a batch of changes for a specific partition.
    /// </summary>
    /// <param name="partitionKey">The partition key.</param>
    /// <param name="changes">The changed document.</param>
    /// <param name="cancellationToken">(Optional) <seealso cref="CancellationToken"/> representing request cancellation.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    Task ProcessAsync(
        string partitionKey,
        IReadOnlyCollection<T> changes,
        CancellationToken cancellationToken);

    /// <summary>
    /// Delegate to notify errors during change feed operations.
    /// </summary>
    /// <param name="leaseToken">A unique identifier for the lease.</param>
    /// <param name="exception">The exception that happened.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    Task ErrorAsync(
        string leaseToken,
        Exception exception);
}
