using System.Collections.Immutable;
using Chronicles.EventStore;

namespace Chronicles.Cqrs;

public interface ICommandCompletionContext<TCommand>
    where TCommand : class
{
    /// <summary>
    /// The command that was executed.
    /// </summary>
    TCommand Command { get; }

    /// <summary>
    /// Gets the current state of the stream.
    /// </summary>
    StreamMetadata Metadata { get; }

    /// <summary>
    /// Gets or sets the response of the command.
    /// </summary>
    /// <remarks>
    /// Use this property to override the response provided by the command handler.
    /// </remarks>
    object? Response { get; set; }

    /// <summary>
    /// Gets the state context used when executing the command.
    /// </summary>
    IStateContext State { get; }

    /// <summary>
    /// List of events that were committed to the stream.
    /// </summary>
    IImmutableList<StreamEvent> Events { get; }
}
