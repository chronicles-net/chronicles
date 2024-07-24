using Chronicles.Cqrs.Internal;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Microsoft.Extensions.DependencyInjection;

public static class BuilderExtensions
{
    public static EventStoreBuilder UseCommands(
        this EventStoreBuilder eventStoreBuilder,
        Action<CommandBuilder> builder)
    {
        builder.Invoke(
            new CommandBuilder(
                eventStoreBuilder.StoreName,
                eventStoreBuilder.Services));

        eventStoreBuilder.Services.TryAddSingleton<ICommandExecutorFactory, CommandExecutorFactory>();

        return eventStoreBuilder;
    }
}
