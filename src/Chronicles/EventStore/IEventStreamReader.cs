namespace Chronicles.EventStore;

/// <summary>
/// Provides functionality to read events, metadata and checkpoints from an event stream.
/// </summary>
public interface IEventStreamReader
{
    /// <summary>
    ///   Read events from stream.
    /// </summary>
    /// <param name="streamId">Event stream to read from.</param>
    /// <param name="options">(Optional) Specify read options to only include certain events, and/or ensure stream is at a given version.</param>
    /// <param name="storeName">(Optional) Name of the configured document store.</param>
    /// <param name="cancellationToken">(Optional) <seealso cref="CancellationToken"/> representing request cancellation.</param>
    /// <returns>List of <seealso cref="StreamEvent"/> from stream.</returns>
    IAsyncEnumerable<StreamEvent> ReadAsync(
        StreamId streamId,
        StreamReadOptions? options = default,
        string? storeName = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///   Gets current state for a specific stream.
    /// </summary>
    /// <remarks>
    ///   If the stream is not found the <see cref="StreamMetadata.State"/> is <see cref="StreamState.New"/>.
    /// </remarks>
    /// <param name="streamId">Event stream to read from.</param>
    /// <param name="storeName">(Optional) Name of the configured document store.</param>
    /// <param name="cancellationToken">(Optional) <seealso cref="CancellationToken"/> representing request cancellation.</param>
    /// <returns>Stream <seealso cref="StreamMetadata"/> information.</returns>
    Task<StreamMetadata> GetMetadataAsync(
        StreamId streamId,
        string? storeName = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///   Search for streams matching a given filter expression.
    /// </summary>
    /// <param name="filter">
    ///   Filter expression for finding desired streams.
    ///   <seealso href="https://devblogs.microsoft.com/cosmosdb/like-keyword-cosmosdb/"/>
    /// </param>
    /// <param name="createdAfter">(Optional) exclude streams created prior to this timestamp.</param>
    /// <param name="storeName">(Optional) Name of the configured document store.</param>
    /// <param name="cancellationToken">(Optional) <seealso cref="CancellationToken"/> representing request cancellation.</param>
    /// <returns>List of stream meta-data found.</returns>
    IAsyncEnumerable<StreamMetadata> QueryStreamsAsync(
        string? filter = default,
        DateTimeOffset? createdAfter = default,
        string? storeName = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///   Gets a named checkpoint with state from a stream.
    /// </summary>
    /// <typeparam name="TState">Type of state.</typeparam>
    /// <param name="name">Name of checkpoint.</param>
    /// <param name="streamId">Id of stream.</param>
    /// <param name="storeName">(Optional) Name of the configured document store.</param>
    /// <param name="cancellationToken">(Optional) <seealso cref="CancellationToken"/> representing request cancellation.</param>
    /// <returns>A state full <see cref="Checkpoint{TState}"/> or <c>null</c> if not found.</returns>
    Task<Checkpoint<TState>?> GetCheckpointAsync<TState>(
        string name,
        StreamId streamId,
        string? storeName = null,
        CancellationToken cancellationToken = default)
        where TState : class;
}