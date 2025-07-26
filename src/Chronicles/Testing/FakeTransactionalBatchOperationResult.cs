using System.Net;
using Microsoft.Azure.Cosmos;

namespace Chronicles.Testing;

public class FakeTransactionalBatchOperationResult<T>
    : TransactionalBatchOperationResult<T>
{
    private readonly bool isSuccess;
    private readonly HttpStatusCode statusCode;

    public FakeTransactionalBatchOperationResult(
        T? resource,
        FakeDocumentAction action,
        bool isSuccess = true,
        HttpStatusCode statusCode = HttpStatusCode.OK)
    {
        if (resource != null)
        {
            Resource = resource;
        }

        Action = action;
        this.isSuccess = isSuccess;
        this.statusCode = statusCode;
    }

    public override bool IsSuccessStatusCode => isSuccess;

    public override HttpStatusCode StatusCode => statusCode;

    public FakeDocumentAction Action { get; }

    public FakeTransactionalBatchOperationResult<TResult> AsType<TResult>()
        => new FakeTransactionalBatchOperationResult<TResult>(
            Resource is TResult { } r ? r : default,
            Action,
            IsSuccessStatusCode,
            StatusCode);
}
