namespace Chronicles.Documents;

public abstract class DocumentPartitionProcessor<T> : IDocumentProcessor<T>
{
    private readonly int maxDegreeOfParallelism;

    protected DocumentPartitionProcessor(
        int maxDegreeOfParallelism = 1)
    {
        this.maxDegreeOfParallelism = maxDegreeOfParallelism;
    }

    public abstract Task ErrorAsync(
        string leaseToken,
        Exception exception);

    public virtual string GetPartitionKey(T document)
        => document switch
        {
            IDocument d => d.GetPartitionKey(),
            _ => throw new InvalidOperationException(
                $"Document type does not implement {nameof(IDocument)}, " +
                $"and {nameof(GetPartitionKey)} is not implemented on processor."),
        };

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

    protected abstract Task ProcessAsync(
        string partitionKey,
        IReadOnlyCollection<T> changes,
        CancellationToken cancellationToken);
}
