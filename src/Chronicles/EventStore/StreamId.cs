namespace Chronicles.EventStore;

/// <summary>
/// Represents a unique identifier for an event stream, composed of a category and an id.
/// </summary>
public record StreamId
{
    private static readonly char[] Separator = ['.'];

    /// <summary>
    /// Gets an empty <see cref="StreamId"/> instance with empty category and id.
    /// </summary>
    public static readonly StreamId Empty = new(string.Empty, string.Empty);

    private readonly string value;

    /// <summary>
    /// Initializes a new instance of the <see cref="StreamId"/> record with the specified category and id.
    /// Use this constructor for simple stream identifiers.
    /// </summary>
    /// <param name="category">The category of the stream.</param>
    /// <param name="id">The unique id of the stream.</param>
    public StreamId(
        string category,
        string id)
    {
        Category = category;
        Id = id;
        value = $"{Category}.{Id}";
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="StreamId"/> record with a category and composite id parts.
    /// Use this constructor for streams identified by multiple key segments.
    /// </summary>
    /// <param name="category">The category of the stream.</param>
    /// <param name="compositeId">The composite id segments for the stream.</param>
    /// <exception cref="ArgumentException">Thrown if no composite id segments are provided.</exception>
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

    /// <summary>
    /// Initializes a new instance of the <see cref="StreamId"/> record from a string representation.
    /// Use this constructor to parse a stream id from its string form.
    /// </summary>
    /// <param name="streamId">The string representation of the stream id.</param>
    private StreamId(string streamId)
    {
        var parts = streamId.Split(Separator, StringSplitOptions.RemoveEmptyEntries);
        Category = parts[0];
        Id = parts[^1];
        value = streamId;
    }

    /// <summary>
    /// Gets the category part of the stream id.
    /// </summary>
    public string Category { get; }

    /// <summary>
    /// Gets the id part of the stream id.
    /// </summary>
    public string Id { get; }

    /// <summary>
    /// Returns the string representation of the stream id.
    /// </summary>
    /// <returns>The string value of the stream id.</returns>
    private string AsString() => value;

    /// <summary>
    /// Converts a <see cref="StreamId"/> to its string representation explicitly.
    /// </summary>
    /// <param name="streamId">The <see cref="StreamId"/> to convert.</param>
    public static explicit operator string(StreamId streamId)
        => streamId.AsString();

    /// <summary>
    /// Creates a <see cref="StreamId"/> from its string representation.
    /// </summary>
    /// <param name="streamId">The string representation of the stream id.</param>
    /// <returns>A <see cref="StreamId"/> instance.</returns>
    public static StreamId FromString(string streamId)
        => new(streamId);

    public virtual bool Equals(StreamId? other)
        => other is not null
        && value == other.value;

    public override int GetHashCode()
    {
        var hash = new HashCode();
        hash.Add(value);
        return hash.ToHashCode();
    }
}
