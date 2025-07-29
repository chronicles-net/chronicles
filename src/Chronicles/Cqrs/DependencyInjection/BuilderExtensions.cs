using Chronicles.Cqrs.Internal;
using Chronicles.EventStore.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Chronicles.Cqrs.DependencyInjection;

/// <summary>
/// Provides extension methods for configuring CQRS in the Chronicles event store pipeline.
/// Use these methods to enable CQRS command processing and register command handlers and processors.
/// </summary>
public static class BuilderExtensions
{
    /// <summary>
    /// Configures the event store builder to use CQRS by registering command processing services and allowing further CQRS configuration.
    /// Use this method when you want to enable CQRS command handling and event sourcing in your application.
    /// </summary>
    /// <param name="eventStoreBuilder">The event store builder to configure.</param>
    /// <param name="builder">An action to configure CQRS command handlers and processors using a <see cref="CqrsBuilder"/>.</param>
    /// <returns>The updated <see cref="EventStoreBuilder"/> instance for further configuration.</returns>
    public static EventStoreBuilder WithCqrs(
        this EventStoreBuilder eventStoreBuilder,
        Action<CqrsBuilder> builder)
    {
        builder.Invoke(
            new CqrsBuilder(
                eventStoreBuilder.StoreName,
                eventStoreBuilder.Services));

        eventStoreBuilder.Services.TryAddSingleton<ICommandExecutorFactory, CommandExecutorFactory>();
        eventStoreBuilder.Services.TryAddSingleton<ICommandProcessorFactory, CommandProcessorFactory>();

        return eventStoreBuilder;
    }
}
