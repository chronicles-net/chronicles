using Chronicles.Cqrs.Internal;
using Chronicles.EventStore;
using Microsoft.Extensions.DependencyInjection;

namespace Chronicles.Cqrs.DependencyInjection;

/// <summary>
/// Provides a builder for configuring CQRS command processing in the Chronicles pipeline.
/// Use this class to register command handlers and processors for handling commands and managing state changes via event sourcing.
/// </summary>
public class CqrsBuilder(
    string storeName,
    IServiceCollection serviceCollection)
{
    /// <summary>
    /// Gets the service collection used for dependency injection.
    /// </summary>
    public IServiceCollection Services { get; } = serviceCollection;

    /// <summary>
    /// Registers a stateless command handler and its processor for the specified command type.
    /// Use this method when your command does not require a dedicated state object and is handled by an <see cref="IStatelessCommandHandler{TCommand}"/>.
    /// </summary>
    /// <typeparam name="TCommand">The type of the command to handle.</typeparam>
    /// <typeparam name="THandler">The type of the stateless command handler.</typeparam>
    /// <param name="options">Optional command execution options, such as conflict behavior and consistency requirements.</param>
    /// <returns>The updated <see cref="CqrsBuilder"/> instance.</returns>
    public CqrsBuilder AddCommand<TCommand, THandler>(
        CommandOptions? options = null)
        where TCommand : class
        where THandler : class, IStatelessCommandHandler<TCommand>
    {
        Services.AddTransient<THandler>();
        Services.AddTransient<ICommandExecutor<TCommand>>(s =>
            new StatelessCommandExecutor<TCommand, THandler>(
                s.GetRequiredService<THandler>()));
        Services.AddTransient<ICommandProcessor<TCommand>>(s =>
            new CommandProcessor<TCommand>(
                storeName,
                options ?? new CommandOptions(),
                s.GetRequiredService<IEventStreamReader>(),
                s.GetRequiredService<IEventStreamWriter>(),
                s.GetRequiredService<ICommandExecutorFactory>()));
        return this;
    }

    /// <summary>
    /// Registers a stateful command handler and its processor for the specified command and state types.
    /// Use this method when your command requires a dedicated state object and is handled by an <see cref="ICommandHandler{TCommand, TState}"/>.
    /// </summary>
    /// <typeparam name="TCommand">The type of the command to handle.</typeparam>
    /// <typeparam name="THandler">The type of the stateful command handler.</typeparam>
    /// <typeparam name="TState">The type of the state associated with the command.</typeparam>
    /// <param name="options">Optional command execution options, such as conflict behavior and consistency requirements.</param>
    /// <returns>The updated <see cref="CqrsBuilder"/> instance.</returns>
    public CqrsBuilder AddCommand<TCommand, THandler, TState>(
        CommandOptions? options = null)
        where TCommand : class
        where TState : class
        where THandler : class, ICommandHandler<TCommand, TState>
    {
        Services.AddTransient<THandler>();
        Services.AddTransient<ICommandExecutor<TCommand>>(s =>
            new StatefulCommandExecutor<TCommand, THandler, TState>(
                s.GetRequiredService<THandler>()));
        Services.AddTransient<ICommandProcessor<TCommand>>(s =>
            new CommandProcessor<TCommand>(
                storeName,
                options ?? new CommandOptions(),
                s.GetRequiredService<IEventStreamReader>(),
                s.GetRequiredService<IEventStreamWriter>(),
                s.GetRequiredService<ICommandExecutorFactory>()));
        return this;
    }
}
