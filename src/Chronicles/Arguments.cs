using System.Diagnostics;
using Chronicles.EventStore;

namespace Chronicles;

[DebuggerStepThrough]
internal static class Arguments
{
    internal static IReadOnlyCollection<object> EnsureNoNullValues(IReadOnlyCollection<object> events, string argumentName)
    {
        if (events is null)
        {
            throw new ArgumentNullException(argumentName);
        }

        if (events.Any(e => e is null))
        {
            throw new ArgumentException("Null values not allowed", argumentName);
        }

        return events;
    }

    internal static object EnsureNotNull(object argumentValue, string argumentName)
    {
        if (argumentValue is null)
        {
            throw new ArgumentNullException(argumentName);
        }

        return argumentValue;
    }

    internal static T EnsureNotNull<T>(T? argumentValue, string argumentName)
    {
        if (argumentValue is null)
        {
            throw new ArgumentNullException(argumentName);
        }

        return argumentValue;
    }

    internal static StreamVersion EnsureValueRange(StreamVersion streamVersion, string argumentName)
    {
        if (streamVersion < StreamVersion.Any)
        {
            throw new ArgumentOutOfRangeException(
                argumentName,
                $"Stream version {streamVersion.Value} is outside of valid range [< 0].");
        }

        return streamVersion;
    }

    internal static long EnsureVersionRange(long streamVersion, string argumentName)
    {
        if (streamVersion < StreamVersion.RequireNotEmptyValue)
        {
            throw new ArgumentOutOfRangeException(
                argumentName,
                $"Stream version {streamVersion} is outside of valid range [..].");
        }

        return streamVersion;
    }
}