using System.Net;
using Microsoft.Azure.Cosmos;

namespace Chronicles.Cosmos.Testing;

public class FakeTransactionalBatchOperationResult<T> : TransactionalBatchOperationResult<T>
{
    private readonly bool isSuccess;
    private readonly HttpStatusCode statusCode;

    public FakeTransactionalBatchOperationResult(
        T? resource,
        bool isSuccess = true,
        HttpStatusCode statusCode = HttpStatusCode.OK)
    {
        if (resource != null)
        {
            Resource = resource;
        }

        this.isSuccess = isSuccess;
        this.statusCode = statusCode;
    }

    public override bool IsSuccessStatusCode => isSuccess;

    public override HttpStatusCode StatusCode => statusCode;

    public override string? ETag => Resource is ICosmosDocument d ? d.ETag : null!;
}
