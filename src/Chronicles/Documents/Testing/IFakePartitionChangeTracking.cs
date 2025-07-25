using System.Collections.Immutable;

namespace Chronicles.Documents.Testing;

public interface IFakePartitionChangeTracking
{
    Task PartitionChangedAsync(
        string partitionKey,
        ImmutableList<FakeDocumentOperation> changes);
}
