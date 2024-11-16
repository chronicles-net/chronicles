using Chronicles.Cqrs.Internal;
using Chronicles.EventStore;
using Microsoft.Extensions.DependencyInjection;

namespace Chronicles.Cqrs.DependencyInjection;

public class CqrsBuilder(
    string storeName,
    IServiceCollection serviceCollection)
{
    public IServiceCollection Services { get; } = serviceCollection;

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
