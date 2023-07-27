using System.ComponentModel;

namespace Chronicles.EventStore;

/// <summary>
/// {tenant}.{stream}
/// </summary>
public readonly struct StreamId : IEquatable<StreamId>
{
    public static readonly StreamId Empty = new StreamId(string.Empty);

    public StreamId(string streamId)
        => Value = streamId;

    /// <summary>
    /// Gets the fully qualified stream id as a string.
    /// </summary>
    public string Value { get; }

    public static implicit operator StreamId(string id)
        => new(id);

    public static explicit operator string(StreamId streamId)
        => streamId.Value;

    public static bool operator ==(StreamId left, StreamId right)
        => string.Equals(left.Value, right.Value, StringComparison.Ordinal);

    public static bool operator !=(StreamId left, StreamId right)
        => !string.Equals(left.Value, right.Value, StringComparison.Ordinal);

    public static StreamId ToStreamId(string streamId)
        => new(streamId);

    public static string FromStreamId(StreamId streamId)
        => streamId.Value;

    public static bool Equals(StreamId left, StreamId right)
        => left.Equals(right);

    [EditorBrowsable(EditorBrowsableState.Never)]
    public override bool Equals(object? obj)
        => obj is StreamId id && Equals(id);

    public bool Equals(StreamId other)
        => string.Equals(Value, other.Value, StringComparison.Ordinal);

    [EditorBrowsable(EditorBrowsableState.Never)]
    public override int GetHashCode()
        => HashCode.Combine(Value);
}