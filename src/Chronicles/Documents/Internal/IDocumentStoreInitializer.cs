namespace Chronicles.Documents.Internal;

internal interface IDocumentStoreInitializer
{
    Task InitializeAsync(CancellationToken cancellationToken);
}
