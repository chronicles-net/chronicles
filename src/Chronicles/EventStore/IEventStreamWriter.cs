using System.Collections.Immutable;

namespace Chronicles.EventStore;

public interface IEventStreamWriter
{
    /// <summary>
    ///   Writes a collection of event objects to an event stream.
    /// </summary>
    /// <exception cref="StreamConflictException">
    ///   Is thrown when writing to a stream encounters a conflict.
    /// </exception>
    /// <param name="streamId">Event stream to write events too.</param>
    /// <param name="events">Collection of event objects to write.</param>
    /// <param name="options">(Optional) The options for writing events.</param>
    /// <param name="storeName">(Optional) Name of the configured document store.</param>
    /// <param name="cancellationToken">(Optional) <seealso cref="CancellationToken"/> representing request cancellation.</param>
    /// <returns><see cref="StreamWriteResult"/> after write operation.</returns>
    Task<StreamWriteResult> WriteAsync(
        StreamId streamId,
        IImmutableList<object> events,
        StreamWriteOptions? options = default,
        string? storeName = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///   Will close an <seealso cref="StreamState.Active"/> stream and prevent further writes.
    /// </summary>
    /// <remarks>
    ///   If the stream is not <seealso cref="StreamState.Active"/> or <seealso cref="StreamState.Closed"/>
    ///   this operation will throw a <see cref="StreamConflictException"/>.
    /// </remarks>
    /// <param name="streamId">Event stream to close.</param>
    /// <param name="storeName">(Optional) Name of the configured document store.</param>
    /// <param name="cancellationToken">(Optional) <seealso cref="CancellationToken"/> representing request cancellation.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    Task CloseAsync(
        StreamId streamId,
        string? storeName = null,
        CancellationToken cancellationToken = default);

    ////Task DeleteStreamAsync(
    ////    StreamId streamId,
    ////    CancellationToken cancellationToken = default);

    ////Task ArchiveStreamAsync(
    ////    StreamId streamId,
    ////    CancellationToken cancellationToken = default);

    /// <summary>
    ///   Sets a named checkpoint at a given version in the stream.
    /// </summary>
    /// <remarks>
    ///   Only one checkpoint per name can exists at any given time.
    ///   A checkpoint will be overridden when using an existing name.
    /// </remarks>
    /// <exception cref="StreamConflictException">
    ///   Is thrown if the <paramref name="version"/> is out of range
    ///   or the stream is <see cref="StreamState.New"/>.
    /// </exception>
    /// <param name="name">Name of checkpoint.</param>
    /// <param name="streamId">Id of stream.</param>
    /// <param name="version">Version within the stream this checkpoint is related too.</param>
    /// <param name="state">(Optional) State object to store along side the checkpoint.</param>
    /// <param name="storeName">(Optional) Name of the configured document store.</param>
    /// <param name="cancellationToken">(Optional) <seealso cref="CancellationToken"/> representing request cancellation.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    Task SetCheckpointAsync(
        string name,
        StreamId streamId,
        StreamVersion version,
        object? state = default,
        string? storeName = null,
        CancellationToken cancellationToken = default);
}
