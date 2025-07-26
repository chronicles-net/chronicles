using Chronicles.Documents;
using Chronicles.Documents.Internal;
using Microsoft.Azure.Cosmos;

namespace Chronicles.Testing;

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
