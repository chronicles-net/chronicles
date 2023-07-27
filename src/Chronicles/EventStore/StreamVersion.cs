using System.ComponentModel;

namespace Chronicles.EventStore;

/// <summary>
/// Represents a position aka version within an event stream.
/// </summary>
public readonly struct StreamVersion :
    IComparable<StreamVersion>,
    IEquatable<StreamVersion>
{
    /// <summary>
    /// Static representation of the start of a stream.
    /// </summary>
    /// <remarks>
    /// Use this when you require the stream to be empty before writing to it.
    /// </remarks>
    public static readonly StreamVersion StartOfStream = new(StartOfStreamValue);

    /// <summary>
    /// Use this when you want to append events to a stream whether it is empty or not.
    /// </summary>
    public static readonly StreamVersion Any = new(AnyValue);

    /// <summary>
    /// Represents a stream version that is not empty.
    /// </summary>
    /// <remarks>
    /// Use this when you require the stream to contain at least one event before writing to it.
    /// </remarks>
    public static readonly StreamVersion NotEmpty = new(NotEmptyValue);

    internal const long NotEmptyValue = -1;
    internal const long StartOfStreamValue = 0;
    internal const long EndOfStreamValue = long.MaxValue;
    internal const long AnyValue = long.MaxValue;

    internal StreamVersion(long version)
        => Value = Arguments.EnsureVersionRange(version, nameof(version));

    /// <summary>
    /// Gets the value of the stream version.
    /// </summary>
    public long Value { get; }

    public static implicit operator StreamVersion(long version)
        => new(version);

    public static explicit operator long(StreamVersion version)
        => version.Value;

    public static bool operator >(StreamVersion left, StreamVersion right)
        => left.Value > right.Value;

    public static bool operator <=(StreamVersion left, StreamVersion right)
        => left.Value <= right.Value;

    public static bool operator >=(StreamVersion left, StreamVersion right)
        => left.Value >= right.Value;

    public static bool operator <(StreamVersion left, StreamVersion right)
        => left.Value < right.Value;

    public static bool operator ==(StreamVersion left, StreamVersion right)
        => left.Value == right.Value;

    public static bool operator !=(StreamVersion left, StreamVersion right)
        => left.Value != right.Value;

    public static StreamVersion ToStreamVersion(long version)
        => new(version);

    public static long FromStreamVersion(StreamVersion version)
        => version.Value;

    /// <summary>
    ///   Compares this instance to a specified <seealso cref="StreamVersion"/> and returns an indication of their relative values.
    /// </summary>
    /// <param name="other">A StreamVersion to compare.</param>
    /// <returns>
    ///   A value that indicates the relative order of the objects being compared. The
    ///   return value has these meanings: Value Meaning Less than zero This instance precedes
    ///   other in the sort order. Zero This instance occurs in the same position in the
    ///   sort order as other. Greater than zero This instance follows other in the sort order.
    /// </returns>
    public readonly int CompareTo(StreamVersion other)
        => Value.CompareTo(other.Value);

    [EditorBrowsable(EditorBrowsableState.Never)]
    public override readonly bool Equals(object? obj)
        => obj is StreamVersion version && Equals(version);

    public readonly bool Equals(StreamVersion other)
        => Value == other.Value;

    [EditorBrowsable(EditorBrowsableState.Never)]
    public override readonly int GetHashCode()
        => HashCode.Combine(Value);
}