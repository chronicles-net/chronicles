using Microsoft.Extensions.DependencyInjection;

namespace Chronicles.Cqrs.Internal;

internal class CommandProcessorFactory(
    IServiceProvider service)
    : ICommandProcessorFactory
{
    public ICommandProcessor<TCommand> Create<TCommand>()
        where TCommand : class
        => service.GetRequiredService<ICommandProcessor<TCommand>>();
}
