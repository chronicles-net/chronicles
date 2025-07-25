using Microsoft.Azure.Cosmos;

namespace Chronicles.Documents.Testing;

public class FakeChangeFeedProcessor<T>(
    FakeDocumentStore store,
    Container.ChangesHandler<T> onChanges,
    Container.ChangeFeedMonitorErrorDelegate? onError = null)
    : ChangeFeedProcessor
    where T : class
{
    private readonly string registrationId = Guid.NewGuid().ToString();

    public override Task StartAsync()
    {
        store
            .GetContainer<T>()
            .RegisterChangeNotification(
                registrationId,
                async (partition, changes) =>
                {
                    var changesList = changes
                        .Where(change => change.Action != FakeDocumentAction.Delete)
                        .Select(change => change.Document.DeepCloneObject<T>(store.SerializerOptions))
                        .OfType<T>()
                        .ToList();

                    try
                    {
                        await onChanges.Invoke(changesList, CancellationToken.None);
                    }
                    catch (Exception ex)
                    {
                        if (onError is { })
                        {
                            await onError.Invoke(registrationId, ex);
                        }
                    }
                });

        return Task.CompletedTask;
    }

    public override Task StopAsync()
    {
        store
            .GetContainer<T>()
            .UnregisterChangeNotification(registrationId);

        return Task.CompletedTask;
    }
}
