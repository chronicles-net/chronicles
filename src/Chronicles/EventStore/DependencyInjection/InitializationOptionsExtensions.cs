using Chronicles.Documents;
using Chronicles.EventStore.Internal;
using Microsoft.Azure.Cosmos;

namespace Chronicles.EventStore.DependencyInjection;

public static class InitializationOptionsExtensions
{
    public static InitializationOptions CreateEventStore(
        this InitializationOptions options,
        ThroughputProperties? eventStoreThroughput = null,
        ThroughputProperties? streamIndexThroughput = null)
        => options
            .CreateSubscriptionContainer()
            .CreateContainer<EventDocumentBase>(
                p =>
                {
                    p.IndexingPolicy = new IndexingPolicy
                    {
                        Automatic = true,
                        IndexingMode = IndexingMode.Consistent,
                    };
                    p.PartitionKeyPath = "/pk";
                    p.IndexingPolicy.IncludedPaths.Add(new IncludedPath { Path = "/*" });
                    p.IndexingPolicy.ExcludedPaths.Add(new ExcludedPath { Path = "/data/*" });
                    p.IndexingPolicy.CompositeIndexes.Add([.. new[]
                        {
                          new CompositePath { Path = "/pk", Order = CompositePathSortOrder.Ascending },
                          new CompositePath { Path = "/properties/version", Order = CompositePathSortOrder.Ascending },
                        }.ToList()]);
                },
                eventStoreThroughput)
            .CreateContainer<Checkpoint>(
                p =>
                {
                    p.IndexingPolicy = new IndexingPolicy
                    {
                        Automatic = true,
                        IndexingMode = IndexingMode.Consistent,
                    };
                    p.PartitionKeyPath = "/pk";
                },
                streamIndexThroughput);
}
