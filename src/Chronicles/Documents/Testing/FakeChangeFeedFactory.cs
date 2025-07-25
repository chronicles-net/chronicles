using Chronicles.Documents.Internal;
using Microsoft.Azure.Cosmos;

namespace Chronicles.Documents.Testing;

public class FakeChangeFeedFactory(
    IFakeDocumentStoreProvider storeProvider)
    : IChangeFeedFactory
{
    public ChangeFeedProcessor Create<T>(
        string? storeName,
        string subscriptionName,
        Container.ChangesHandler<T> onChanges,
        Container.ChangeFeedMonitorErrorDelegate? onError = null)
        where T : class
        => new FakeChangeFeedProcessor<T>(
            storeProvider.GetStore(storeName ?? DocumentOptions.DefaultStoreName),
            onChanges,
            onError);
}
