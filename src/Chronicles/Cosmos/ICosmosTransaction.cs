namespace Chronicles.Cosmos;

public interface ICosmosTransaction<T>
    where T : ICosmosDocument
{
    ICosmosTransaction<T> Write(T doc);

    ICosmosTransaction<T> Replace(T doc);

    ICosmosTransaction<T> Create(T doc);

    ICosmosTransaction<T> Delete(string id, string partitionKey);

    Task CommitAsync(CancellationToken cancellationToken);
}
