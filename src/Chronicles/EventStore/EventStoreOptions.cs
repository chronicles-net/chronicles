using Microsoft.Extensions.Options;

namespace Chronicles.EventStore;

/// <summary>
/// Provides configuration options for the event store in Chronicles, including container names and document store association.
/// Use this class to specify which containers to use for event storage and stream indexing, and to associate the event store with a document store.
/// </summary>
public class EventStoreOptions
{
    /// <summary>
    /// Gets or sets the name of the document store associated with the event store.
    /// Set this property to specify which document store to use for event sourcing and projections.
    /// </summary>
    public string DocumentStoreName { get; set; } = Options.DefaultName;

    /// <summary>
    /// Gets or sets the name of the container used for storing events.
    /// Set this property to specify where event data will be persisted in Cosmos DB.
    /// </summary>
    public string EventStoreContainer { get; set; } = "event-store";

    /// <summary>
    /// Gets or sets the name of the container used for stream indexes and checkpoints.
    /// Set this property to specify where stream metadata and checkpoint data will be stored.
    /// </summary>
    public string StreamIndexContainer { get; set; } = "stream-index";
}
