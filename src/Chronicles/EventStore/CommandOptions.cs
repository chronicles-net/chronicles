namespace Chronicles.EventStore;

/// <summary>
/// Options when executing a command.
/// </summary>
public class CommandOptions
{
    /// <summary>
    /// Unique id of command instance.
    /// </summary>
    public string? CommandId { get; set; }

    /// <summary>
    /// Correlation id used to track a request through various systems and services.
    /// </summary>
    public string? CorrelationId { get; set; }

    /// <summary>
    ///   (Optional) Set the required version of the stream.
    /// </summary>
    public StreamVersion? RequiredVersion { get; set; }

    /// <summary>
    /// (Optional) required stream state.
    /// </summary>
    public StreamState? RequiredState { get; set; }

    /// <summary>
    /// Behavior when a stream conflict occurs.
    /// </summary>
    public OnConflict Behavior { get; set; } = OnConflict.Fail;

    /// <summary>
    /// Gets or sets the number of times to rerun or retry the command when receiving a conflict.
    /// </summary>
    /// <remarks>Default count is 3</remarks>
    public int BehaviorCount { get; set; } = 3;
}