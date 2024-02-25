using Chronicles.EventStore.Internal.EventConsumers;
using Chronicles.EventStore.Internal.Streams;

namespace Chronicles.EventStore.Internal.Commands;

internal class CommandProcessor<TCommand>(
    IStreamEventWriter eventWriter,
    IStreamEventReader eventReader,
    IStreamMetadataReader metadataReader,
    ICommandHandlerFactory factory,
    EventConsumerStateReflector<TCommand> reflector)
    : ICommandProcessor<TCommand>
    where TCommand : class
{
    public async ValueTask<CommandResult> ExecuteAsync(
        TCommand command,
        StreamId streamId,
        string? storeName,
        CancellationToken cancellationToken)
        => await SafeExecuteAsync(
                command,
                streamId,
                options: null,
                storeName,
                cancellationToken)
            .ConfigureAwait(false);

    private async ValueTask<CommandResult> SafeExecuteAsync(
        TCommand command,
        StreamId streamId,
        CommandOptions? options,
        string? storeName,
        CancellationToken cancellationToken)
    {
        var metadata = await metadataReader
            .GetAsync(streamId, storeName, cancellationToken)
            .ConfigureAwait(false);

        var handler = factory.Create<TCommand>();

        // Ensure we only configure options once.
        if (options is null)
        {
            options = new CommandOptions();
            handler.Configure(metadata, command, options);
        }

        var constraints = options.GetStreamReadOptions();
        metadata.EnsureSuccess(constraints);
        var stateContext = new EventConsumerStateContext(handler);

        if (!reflector.IsNotConsumingEvents())
        {
            try
            {
                var events = eventReader
                    .ReadAsync(streamId, constraints, metadata, storeName, cancellationToken)
                    .ConfigureAwait(false);
                await foreach (var evt in events)
                {
                    await reflector
                        .ConsumeAsync(evt, handler, stateContext, cancellationToken)
                        .ConfigureAwait(false);
                }
            }
            catch (StreamConflictException readConflict)
            {
                return new CommandResult(
                    readConflict.StreamId,
                    readConflict.Version,
                    ResultType.Conflict,
                    null);
            }
        }

        var context = new CommandContext<TCommand>(metadata, stateContext, command);
        await handler
            .ExecuteAsync(context, cancellationToken)
            .ConfigureAwait(false);

        if (context.Events.Count == 0)
        {
            // Command did not yield any events
            return new CommandResult(
                metadata.StreamId,
                metadata.Version,
                ResultType.NotModified,
                context.ResponseObject);
        }

        try
        {
            var result = await eventWriter
                .WriteAsync(
                    streamId,
                    [.. context.Events],
                    options.GetStreamWriteOptions(),
                    storeName,
                    cancellationToken)
                .ConfigureAwait(false);

            return new CommandResult(
                result.StreamId,
                result.Version,
                ResultType.Changed,
                context.ResponseObject);
        }
        catch (StreamConflictException conflict)
        {
            var rerunCount = options.NextRerunCount();
            if (rerunCount > 0)
            {
                return await
                    SafeExecuteAsync(command, streamId, options, storeName, cancellationToken)
                   .ConfigureAwait(false);
            }

            return new CommandResult(
                conflict.StreamId,
                conflict.Version,
                ResultType.Conflict);
        }
    }
}