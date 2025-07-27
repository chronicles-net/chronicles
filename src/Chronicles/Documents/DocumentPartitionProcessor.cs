namespace Chronicles.Documents;

/// <summary>
/// Provides a base implementation for partitioned document processors in Chronicles.
/// Implement this class to efficiently process batches of document changes grouped by partition key, with support for parallelism.
/// Use when you need to process large volumes of changes in a distributed or partitioned Cosmos DB environment.
/// </summary>
/// <typeparam name="T">The type of document to be processed.</typeparam>
public abstract class DocumentPartitionProcessor<T> : IDocumentProcessor<T>
{
    private readonly int maxDegreeOfParallelism;

    /// <summary>
    /// Initializes a new instance of the <see cref="DocumentPartitionProcessor{T}"/> class.
    /// </summary>
    /// <param name="maxDegreeOfParallelism">The maximum number of partitions to process in parallel. Default is 1 (sequential).</param>
    protected DocumentPartitionProcessor(
        int maxDegreeOfParallelism = 1)
    {
        this.maxDegreeOfParallelism = maxDegreeOfParallelism;
    }

    /// <summary>
    /// Handles errors that occur during change feed processing for a given lease token.
    /// Implement this method to log, retry, or otherwise handle errors in partition processing.
    /// </summary>
    /// <param name="leaseToken">A unique identifier for the lease/partition.</param>
    /// <param name="exception">The exception that occurred.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    public abstract Task ErrorAsync(
        string leaseToken,
        Exception exception);

    /// <summary>
    /// Gets the partition key for the specified document.
    /// Override this method if your document type does not implement <see cref="IDocument"/> or requires custom partitioning logic.
    /// </summary>
    /// <param name="document">The document to extract the partition key from.</param>
    /// <returns>The partition key string.</returns>
    public virtual string GetPartitionKey(T document)
        => document switch
        {
            IDocument d => d.GetPartitionKey(),
            _ => throw new InvalidOperationException(
                $"Document type does not implement {nameof(IDocument)}, " +
                $"and {nameof(GetPartitionKey)} is not implemented on processor."),
        };

    /// <summary>
    /// Processes a batch of document changes, grouping them by partition key and processing each partition in parallel.
    /// </summary>
    /// <param name="changes">The collection of changed documents.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    public async Task ProcessAsync(
        IReadOnlyCollection<T> changes,
        CancellationToken cancellationToken)
    {
        var partitions = changes
            .GroupBy(GetPartitionKey, StringComparer.Ordinal);

        await Parallel.ForEachAsync(
            partitions,
            new ParallelOptions
            {
                MaxDegreeOfParallelism = maxDegreeOfParallelism,
                CancellationToken = cancellationToken,
            },
            async (p, c) => await ProcessAsync(
                p.Key,
                p.ToArray(),
                c));
    }

    /// <summary>
    /// Processes a batch of document changes for a specific partition key.
    /// Implement this method to define how changes for each partition are handled.
    /// </summary>
    /// <param name="partitionKey">The partition key for the batch.</param>
    /// <param name="changes">The collection of changed documents in the partition.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    protected abstract Task ProcessAsync(
        string partitionKey,
        IReadOnlyCollection<T> changes,
        CancellationToken cancellationToken);
}
