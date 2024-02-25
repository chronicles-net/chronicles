using Microsoft.Extensions.DependencyInjection;

namespace Chronicles.EventStore.Internal.Commands;

public class CommandHandlerFactory(
    IServiceProvider serviceProvider)
    : ICommandHandlerFactory
{
    public ICommandHandler<TCommand> Create<TCommand>()
        where TCommand : class
        => serviceProvider.GetRequiredService<ICommandHandler<TCommand>>();
}
