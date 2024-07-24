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
    : StreamId(CategoryName, Id)
{
    public const string CategoryName = "quest";
}

[ContainerName("quest")]
public class QuestDocument : IDocument
{
    required public string Id { get; set; }

    required public string Pk { get; set; }

    public string Name { get; set; } = string.Empty;

    public ICollection<string> Members { get; set; } = [];

    public bool IsClosed { get; set; }

    public string GetDocumentId() => Id;

    public string GetPartitionKey() => Pk;
}