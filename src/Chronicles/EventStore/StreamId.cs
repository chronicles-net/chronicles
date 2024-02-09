namespace Chronicles.EventStore;

public record StreamId
{
    private static readonly char[] Separator = ['.'];
    public static readonly StreamId Empty = new(string.Empty, string.Empty);

    private readonly string value;

    public StreamId(
        string category,
        string id)
    {
        Category = category;
        Id = id;
        value = $"{Category}.{Id}";
    }

    protected StreamId(
        string category,
        params string[] compositeId)
    {
        if (compositeId.Length == 0)
        {
            throw new ArgumentException("Composite id parameters not provided", nameof(compositeId));
        }

        Category = category;
        Id = compositeId[^1];
        value = $"{Category}.{string.Join('.', compositeId)}";
    }

    private StreamId(string streamId)
    {
        var parts = streamId.Split(Separator, StringSplitOptions.RemoveEmptyEntries);
        Category = parts[0];
        Id = parts[^1];
        value = streamId;
    }

    public string Category { get; }

    public string Id { get; }

    public override string ToString() => value;

    public static explicit operator string(StreamId streamId)
        => streamId.ToString();

    public static StreamId FromString(string streamId)
        => new(streamId);
}
