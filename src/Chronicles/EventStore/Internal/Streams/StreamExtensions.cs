namespace Chronicles.EventStore.Internal.Streams;

internal static class StreamExtensions
{
    public static StreamMetadata EnsureSuccess(
        this StreamMetadata metadata,
        StreamOptions? options)
    {
        if (options?.RequiredState is { } requiredState
        && metadata.State != requiredState)
        {
            throw new StreamConflictException(
                metadata.StreamId,
                metadata.Version,
                metadata.State,
                options?.RequiredVersion,
                options?.RequiredState,
                $"Expecting the stream state to be {options?.RequiredState}, but found {metadata.State}.");
        }

        if (options?.RequiredVersion is { } requiredVersion
        && metadata.Version != requiredVersion)
        {
            throw new StreamConflictException(
                metadata.StreamId,
                metadata.Version,
                metadata.State,
                options?.RequiredVersion,
                options?.RequiredState,
                $"Stream is not at the required version.");
        }

        return metadata;
    }
}
