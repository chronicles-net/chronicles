using Chronicles.EventStore;

namespace Chronicles.Cqrs;

/// <summary>
/// Defines the conflict behavior when executing a command.
/// </summary>
public enum CommandConflictBehavior
{
    /// <summary>
    /// Fail command with a <see cref="StreamConflictException"/>.
    /// </summary>
    Fail,

    /// <summary>
    /// Retry command by executing it again.
    /// The number of retries is defined by <see cref="CommandOptions.Retry"/>.
    /// </summary>
    Retry,
}
