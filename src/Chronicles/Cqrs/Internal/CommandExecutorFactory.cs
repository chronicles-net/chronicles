using Microsoft.Extensions.DependencyInjection;

namespace Chronicles.Cqrs.Internal;

internal class CommandExecutorFactory(
    IServiceProvider serviceProvider)
    : ICommandExecutorFactory
{
    public ICommandExecutor<TCommand> Create<TCommand>()
        where TCommand : class
        => serviceProvider.GetRequiredService<ICommandExecutor<TCommand>>();
}