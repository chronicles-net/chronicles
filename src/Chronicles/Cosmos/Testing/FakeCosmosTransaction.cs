using System.Net;
using Microsoft.Azure.Cosmos;

namespace Chronicles.Cosmos.Testing;

public class FakeCosmosTransaction<T> : ICosmosTransaction<T>
    where T : class, ICosmosDocument
{
    private readonly FakeCosmosWriter<T> writer;
    private readonly string partitionKey;
    private readonly List<Func<FakeCosmosWriter<T>, Task<T?>>> operations = new();

    public FakeCosmosTransaction(
        FakeCosmosWriter<T> writer,
        string partitionKey)
    {
        this.writer = writer;
        this.partitionKey = partitionKey;
    }

    public ICosmosTransaction<T> Create(
        T document,
        TransactionalBatchItemRequestOptions? options = null)
    {
        operations.Add(async w => await w.CreateAsync(document, null));
        return this;
    }

    public ICosmosTransaction<T> Delete(
        string id,
        TransactionalBatchItemRequestOptions? options = null)
    {
        operations.Add(async w =>
        {
            await w.DeleteAsync(id, partitionKey, null);
            return null;
        });
        return this;
    }

    public ICosmosTransaction<T> Replace(
        T document,
        TransactionalBatchItemRequestOptions? options = null)
    {
        operations.Add(async w => await w.ReplaceAsync(document, null));
        return this;
    }

    public ICosmosTransaction<T> Write(
        T document,
        TransactionalBatchItemRequestOptions? options = null)
    {
        operations.Add(async w => await w.WriteAsync(document, null));
        return this;
    }

    public Task<TransactionalBatchResponse> CommitAsync(TransactionalBatchRequestOptions options, CancellationToken cancellationToken)
        => CommitAsync(cancellationToken);

    public async Task<TransactionalBatchResponse> CommitAsync(CancellationToken cancellationToken)
    {
        var results = new List<T?>();
        foreach (var operation in operations)
        {
            results.Add(await operation.Invoke(writer));
        }

        return new FakeResponse(results);
    }

    private sealed class FakeResponse : TransactionalBatchResponse
    {
        private readonly IList<T?> results;

        public FakeResponse(
            IList<T?> results)
        {
            this.results = results;
        }

        public override bool IsSuccessStatusCode => true;

        public override HttpStatusCode StatusCode => HttpStatusCode.OK;

        public override TransactionalBatchOperationResult<TResult> GetOperationResultAtIndex<TResult>(int index)
            => new FakeItemResponse<TResult>(results[index] is TResult r ? r : default);
    }

    private sealed class FakeItemResponse<TResult> : TransactionalBatchOperationResult<TResult>
    {
        public FakeItemResponse(TResult? result)
        {
            if (result != null)
            {
                Resource = result;
            }
        }

        public override bool IsSuccessStatusCode => true;

        public override HttpStatusCode StatusCode => HttpStatusCode.OK;

        public override string? ETag => Resource is ICosmosDocument d ? d.ETag : null!;
    }
}
