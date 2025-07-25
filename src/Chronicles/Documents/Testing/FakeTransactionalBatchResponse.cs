using System.Net;
using Microsoft.Azure.Cosmos;

namespace Chronicles.Documents.Testing;

public class FakeTransactionalBatchResponse : TransactionalBatchResponse
{
    private readonly bool isSuccess;
    private readonly HttpStatusCode statusCode;

    public FakeTransactionalBatchResponse(
        List<FakeTransactionalBatchOperationResult<IDocument>> results,
        bool isSuccess = true,
        HttpStatusCode statusCode = HttpStatusCode.OK)
    {
        Results = results;
        this.isSuccess = isSuccess;
        this.statusCode = statusCode;
    }

    public override bool IsSuccessStatusCode => isSuccess;

    public override HttpStatusCode StatusCode => statusCode;

    public List<FakeTransactionalBatchOperationResult<IDocument>> Results { get; }

    public override TransactionalBatchOperationResult<TResult> GetOperationResultAtIndex<TResult>(
        int index)
        => Results[index].AsType<TResult>();
}
