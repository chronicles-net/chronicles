using Chronicles.Documents;

namespace Chronicles.EventStore.Samples;

public static class QuestEvents
{
    public record QuestStarted(
        string Name);

    public record MembersJoined(
        IReadOnlyCollection<string> Members);
}

public record QuestStreamId(string Id)
    : StreamId("quest", Id);

[ContainerName("quest")]
public class QuestDocument : IDocument
{
    required public string Id { get; set; }

    required public string Pk { get; set; }

    required public string Name { get; set; }

    public IReadOnlyCollection<string> Members { get; } = [];

    public string GetDocumentId() => Id;

    public string GetPartitionKey() => Pk;
}