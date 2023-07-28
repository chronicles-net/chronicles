using System.Net;
using Microsoft.Azure.Cosmos;

namespace Chronicles.Cosmos.Testing;

public class FakeTransactionalBatchResponse<T> : TransactionalBatchResponse
{
    private readonly IList<T?> results;
    private readonly bool isSuccess;
    private readonly HttpStatusCode statusCode;

    public FakeTransactionalBatchResponse(
        IList<T?> results,
        bool isSuccess = true,
        HttpStatusCode statusCode = HttpStatusCode.OK)
    {
        this.results = results;
        this.isSuccess = isSuccess;
        this.statusCode = statusCode;
    }

    public override bool IsSuccessStatusCode => isSuccess;

    public override HttpStatusCode StatusCode => statusCode;

    public override TransactionalBatchOperationResult<TResult> GetOperationResultAtIndex<TResult>(
        int index)
        => new FakeTransactionalBatchOperationResult<TResult>(
            results[index] is TResult r ? r : default,
            isSuccess: true,
            statusCode: HttpStatusCode.OK);
}
