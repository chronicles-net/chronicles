using Chronicles.Cqrs.Internal;
using Chronicles.EventStore.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Chronicles.Cqrs.DependencyInjection;

public static class BuilderExtensions
{
    public static EventStoreBuilder WithCqrs(
        this EventStoreBuilder eventStoreBuilder,
        Action<CqrsBuilder> builder)
    {
        builder.Invoke(
            new CqrsBuilder(
                eventStoreBuilder.StoreName,
                eventStoreBuilder.Services));

        eventStoreBuilder.Services.TryAddSingleton<ICommandExecutorFactory, CommandExecutorFactory>();

        return eventStoreBuilder;
    }
}
