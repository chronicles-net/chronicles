namespace Chronicles.EventStore;

public interface ICommandHandler<TCommand>
    where TCommand : class
{
    /// <summary>
    /// Configure command options.
    /// </summary>
    /// <param name="metadata">Metadata of the stream command handler is being executed at.</param>
    /// <param name="command">Command parameters.</param>
    /// <param name="options">Options to configure.</param>
    void Configure(
        StreamMetadata metadata,
        TCommand command,
        CommandOptions options);

    ValueTask ExecuteAsync(
        ICommandContext<TCommand> context,
        CancellationToken cancellationToken);
}
