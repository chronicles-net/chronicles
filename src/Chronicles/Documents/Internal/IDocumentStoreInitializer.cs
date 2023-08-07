namespace Chronicles.Documents.Internal;

public interface IDocumentStoreInitializer
{
    Task InitializeAsync(CancellationToken cancellationToken);
}