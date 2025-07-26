using Chronicles.Documents.Internal;

namespace Chronicles.Testing;

public class FakeDocumentStoreInitializer
    : IDocumentStoreInitializer
{
    public Task InitializeAsync(
        CancellationToken cancellationToken)
        => Task.CompletedTask;
}
