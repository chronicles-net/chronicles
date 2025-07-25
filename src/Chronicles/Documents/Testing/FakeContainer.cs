using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Net;
using Microsoft.Azure.Cosmos;

namespace Chronicles.Documents.Testing;

public class FakeContainer(
    string name)
    : IFakePartitionChangeTracking
{
    private readonly ConcurrentDictionary<string, FakePartition> partitions = [];
    private readonly Dictionary<string, Func<string, ImmutableList<FakeDocumentOperation>, Task>> changeNotifications = [];

    public string Name => name;

    public FakePartition GetPartition(string partitionKey)
        => partitions.TryGetValue(partitionKey, out var partition)
         ? partition
         : throw new CosmosException(
                $"Partition not found.",
                HttpStatusCode.NotFound,
                0,
                string.Empty,
                0);

    public async Task<bool> DeletePartition(
        string partitionKey)
    {
        if (partitions.TryGetValue(partitionKey, out var partition))
        {
            await partition.DeleteAllDocument();

            return true;
        }

        return false;
    }

    public FakePartition GetOrCreatePartition(string partitionKey)
        => partitions.GetOrAdd(
            partitionKey,
            key => new FakePartition(this, key));

    public ImmutableList<FakePartition> FromAllPartitions()
        => [.. partitions.Values];

    Task IFakePartitionChangeTracking.PartitionChangedAsync(
        string partitionKey,
        ImmutableList<FakeDocumentOperation> changes)
    {
        if (changeNotifications.Count == 0)
        {
            return Task.CompletedTask;
        }

        var tasks = changeNotifications
            .Values
            .Select(onChanges => onChanges(partitionKey, changes));

        return Task.WhenAll(tasks);
    }

    public void RegisterChangeNotification(
        string registrationId,
        Func<string, ImmutableList<FakeDocumentOperation>, Task> onChanges)
        => changeNotifications[registrationId] = onChanges;

    public void UnregisterChangeNotification(string registrationId)
        => changeNotifications.Remove(registrationId, out _);
}
