using Chronicles.EventStore;

namespace Chronicles.Cqrs;

public class CommandRequestOptions
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
    /// Causation id used to track the request that caused the command to be executed.
    /// </summary>
    public string? CausationId { get; set; }

    /// <summary>
    /// (Optional) Set the required version of the stream.
    /// </summary>
    public StreamVersion? RequiredVersion { get; set; }
}