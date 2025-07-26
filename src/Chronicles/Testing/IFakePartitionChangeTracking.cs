using System.Collections.Immutable;

namespace Chronicles.Testing;

public interface IFakePartitionChangeTracking
{
    Task PartitionChangedAsync(
        string partitionKey,
        ImmutableList<FakeDocumentOperation> changes);
}
