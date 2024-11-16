using Chronicles.EventStore;

namespace Chronicles.Cqrs;

/// <summary>
/// Options when executing a command.
/// </summary>
public class CommandOptions
{
    /// <summary>
    /// (Optional) required stream state.
    /// </summary>
    public StreamState? RequiredState { get; set; }

    /// <summary>
    /// Behavior when a stream conflict occurs.
    /// </summary>
    /// <remarks>Default conflict behavior is <seealso cref="CommandConflictBehavior.Fail"/>.</remarks>
    public CommandConflictBehavior ConflictBehavior { get; set; } = CommandConflictBehavior.Fail;

    /// <summary>
    /// Gets or sets the number of times to retry the command when receiving a conflict.
    /// <see cref="ConflictBehavior"/> must be set to <see cref="CommandConflictBehavior.Retry"/> to take effect.
    /// </summary>
    /// <remarks>Default count is 3</remarks>
    public int Retry { get; set; } = 3;

    /// <summary>
    /// Consistency level required to execute the command.
    /// </summary>
    /// <remarks>Default is <see cref="CommandConsistency.ReadWrite"/></remarks>
    public CommandConsistency Consistency { get; set; } = CommandConsistency.ReadWrite;
}