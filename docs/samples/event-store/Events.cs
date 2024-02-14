using Chronicles.Documents;

namespace Chronicles.EventStore.Samples;

public record QuestStreamId(string Id)
    : StreamId("quest", Id);

public record QuestStarted(
    string Name);

[ContainerName("quest")]
public class QuestDocument : IDocument
{
    required public string Id { get; set; }

    required public string Pk { get; set; }

    required public string Name { get; set; }

    public IReadOnlyCollection<string> Members { get; } = Array.Empty<string>();

    public string GetDocumentId() => Id;

    public string GetPartitionKey() => Pk;
}