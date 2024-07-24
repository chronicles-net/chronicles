namespace Chronicles.EventStore;

public static class StreamMetadataExtensions
{
    /// <summary>
    ///   Ensure the stream metadata is in the expected state and at the required position/versioning.
    /// </summary>
    /// <remarks>
    ///   Will throw a <see cref="StreamConflictException"/> if the stream metadata is not in the expected state or at the required version.
    /// </remarks>
    /// <param name="metadata">Stream metadata to validate</param>
    /// <param name="options">Stream constraints</param>
    /// <returns>The validated <see cref="StreamMetadata"/>.</returns>
    /// <exception cref="StreamConflictException">
    ///   if the stream metadata is not in the expected state or at the required version.
    /// </exception>
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
