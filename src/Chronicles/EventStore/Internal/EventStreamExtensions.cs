namespace Chronicles.EventStore.Internal;

internal static class EventStreamExtensions
{
    public static void EnsureNotClosed(
        this StreamMetadata metadata,
        StreamId streamId)
    {
        if (metadata.State != StreamState.Active && metadata.State != StreamState.Closed)
        {
            throw new StreamConflictException(
                streamId,
                metadata.Version,
                metadata.State,
                expectedVersion: null,
                StreamState.Active,
                $"Only Active streams can be closed.");
        }
    }

    public static void EnsureCheckpointSuccess(
        this StreamMetadata metadata,
        StreamId streamId,
        StreamVersion version)
    {
        if (metadata.State != StreamState.Active)
        {
            throw new StreamConflictException(
                streamId,
                metadata.Version,
                metadata.State,
                version,
                StreamState.Active,
                $"Expecting the stream to be active.");
        }

        if (metadata.Version < version)
        {
            throw new StreamConflictException(
                streamId,
                metadata.Version,
                metadata.State,
                version,
                expectedState: null,
                $"Stream version is out of bounce.");
        }
    }
}
