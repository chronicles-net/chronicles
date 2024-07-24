using System.Collections.Immutable;
using Chronicles.EventStore;

namespace Chronicles.Cqrs.Internal;

internal class CommandCompletionContext<TCommand>(
    TCommand command,
    StreamMetadata metadata,
    IImmutableList<StreamEvent> events,
    IStateContext stateContext,
    object? response)
    : ICommandCompletionContext<TCommand>
    where TCommand : class
{
    public TCommand Command { get; } = command;

    public StreamMetadata Metadata { get; set; } = metadata;

    public object? Response { get; set; } = response;

    public IStateContext State { get; } = stateContext;

    public IImmutableList<StreamEvent> Events { get; } = events;
}