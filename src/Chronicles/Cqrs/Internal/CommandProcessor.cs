using Chronicles.EventStore;

namespace Chronicles.Cqrs.Internal;

internal class CommandProcessor<TCommand>(
    string storeName,
    CommandOptions commandOptions,
    IEventStreamReader reader,
    IEventStreamWriter writer,
    ICommandExecutorFactory factory)
    : ICommandProcessor<TCommand>
    where TCommand : class
{
    public async Task<CommandResult> ExecuteAsync(
        StreamId streamId,
        TCommand command,
        CommandRequestOptions? requestOptions,
        CancellationToken cancellationToken)
    {
        var retries = commandOptions.ConflictBehavior == CommandConflictBehavior.Retry
            ? commandOptions.Retry
            : 1;
        do
        {
            try
            {
                var context = await ProcessAsync(
                    streamId,
                    command,
                    requestOptions ?? new CommandRequestOptions(),
                    cancellationToken);

                return new CommandResult(
                    context.Metadata.StreamId,
                    context.Metadata.Version,
                    context.Events.Count == 0
                        ? ResultType.NotModified
                        : ResultType.Changed,
                    context.Response);
            }
            catch (StreamConflictException conflict)
            {
                retries--;
                if (retries <= 0)
                {
                    return new CommandResult(
                        conflict.StreamId,
                        conflict.Version,
                        ResultType.Conflict,
                        conflict.Message);
                }
            }
        }
        while (true);
    }

    protected virtual ValueTask<StreamReadOptions> GetStreamReadOptionsAsync(
        StreamMetadata metadata,
        CommandRequestOptions requestOptions,
        IStateContext state,
        CancellationToken cancellationToken)
    {
        var readOptions = requestOptions.GetStreamReadOptions(metadata, commandOptions);
        metadata.EnsureSuccess(readOptions);

        return ValueTask.FromResult(readOptions);
    }

    private async Task<CommandContext<TCommand>> ProcessAsync(
        StreamId streamId,
        TCommand command,
        CommandRequestOptions requestOptions,
        CancellationToken cancellationToken)
    {
        var metadata = await reader
            .GetMetadataAsync(
                streamId,
                storeName: storeName,
                cancellationToken)
            .ConfigureAwait(false);

        var state = IStateContext.Create();

        // Provide access to command data through the state context.
        state.SetState(command);

        var readOptions = await GetStreamReadOptionsAsync(
                metadata,
                requestOptions,
                state,
                cancellationToken)
            .ConfigureAwait(false);

        var eventReader = reader
            .ReadAsync(
                streamId,
                readOptions,
                storeName,
                cancellationToken);

        var context = new CommandContext<TCommand>(
            command,
            metadata,
            state);

        var executor = factory
            .Create<TCommand>();

        await executor
            .ExecuteAsync(
                eventReader,
                context,
                cancellationToken)
            .ConfigureAwait(false);

        // Only write events if we have any.
        if (context.Events.Count > 0)
        {
            var writeOptions = requestOptions
                .GetStreamWriteOptions(commandOptions, metadata);
            var result = await writer
                .WriteAsync(
                    streamId,
                    [.. context.Events],
                    writeOptions,
                    storeName: storeName,
                    cancellationToken)
                .ConfigureAwait(false);

            context.Metadata = result.Metadata;

            var completionContext = new CommandCompletionContext<TCommand>(
                context,
                command,
                result.Metadata,
                result.Events,
                state);
            await context.OnCompleteAsync(
                completionContext,
                cancellationToken);
        }
        else
        {
            var completionContext = new CommandCompletionContext<TCommand>(
                context,
                command,
                context.Metadata,
                [],
                state);
            await context.OnCompleteAsync(
                completionContext,
                cancellationToken);
        }

        return context;
    }
}
