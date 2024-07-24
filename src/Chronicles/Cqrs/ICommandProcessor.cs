using Chronicles.EventStore;

namespace Chronicles.Cqrs;

/// <summary>
/// Interface to execute a command.
/// </summary>
/// <typeparam name="TCommand">Type of command.</typeparam>
public interface ICommandProcessor<TCommand>
    where TCommand : class
{
    /// <summary>
    /// Executes the command handler associated with <paramref name="command"/>
    /// </summary>
    /// <param name="streamId">Id of the stream to execute the command on.</param>
    /// <param name="command">Command parameters.</param>
    /// <param name="requestOptions">(Optional) request options.</param>
    /// <param name="cancellationToken">(Optional) <seealso cref="CancellationToken"/> representing request cancellation.</param>
    /// <returns>The result of the command execution.</returns>
    Task<CommandResult> ExecuteAsync(
        StreamId streamId,
        TCommand command,
        CommandRequestOptions? requestOptions,
        CancellationToken cancellationToken);
}