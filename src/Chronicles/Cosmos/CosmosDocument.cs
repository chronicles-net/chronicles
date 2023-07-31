namespace Chronicles.Cosmos;

public abstract class CosmosDocument : ICosmosDocument
{
    string ICosmosDocument.GetDocumentId() => GetDocumentId();

    string ICosmosDocument.GetPartitionKey() => GetPartitionKey();

    protected abstract string GetDocumentId();

    protected abstract string GetPartitionKey();
}
