using Chronicles.Cqrs.Internal.EventProcessors;
using Chronicles.Documents;
using Chronicles.EventStore;
using Chronicles.EventStore.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;

namespace Chronicles.Cqrs.DependencyInjection;

public static class EventProcessorBuilderExtensions
{
    public static EventProcessorBuilder AddDocumentProjection<TDocument, TConsumer>(
        this EventProcessorBuilder builder)
        where TDocument : class, IDocument
        where TConsumer : class, IDocumentProjection<TDocument>
    {
        builder.Services.AddKeyedSingleton<TConsumer>(builder.Name);
        builder.Services.AddKeyedSingleton<IEventProcessor>(builder.Name, (s, n) =>
            new DocumentProjectionProcessor<TConsumer, TDocument>(
                s.GetRequiredKeyedService<TConsumer>(n),
                s.GetRequiredService<IDocumentReader<TDocument>>(),
                s.GetRequiredService<IDocumentWriter<TDocument>>()));

        return builder;
    }

    public static EventProcessorBuilder AddStateProjection<TState, TConsumer>(
        this EventProcessorBuilder builder)
        where TState : class
        where TConsumer : class, IStateProjection<TState>
    {
        builder.Services.AddKeyedSingleton<TConsumer>(builder.Name);
        builder.Services.AddKeyedSingleton<IEventProcessor>(builder.Name, (s, n) =>
            new StateProjectionProcessor<TConsumer, TState>(
                s.GetRequiredKeyedService<TConsumer>(n)));

        return builder;
    }

    public static EventProcessorBuilder AddStateConsumer<TState, TConsumer>(
        this EventProcessorBuilder builder)
        where TState : class
        where TConsumer : class, IStateConsumer<TState>
    {
        builder.Services.AddKeyedSingleton<TConsumer>(builder.Name);
        builder.Services.AddKeyedSingleton<IEventProcessor>(builder.Name, (s, n) =>
            new StateConsumerProcessor<TConsumer, TState>(
                s.GetRequiredKeyedService<TConsumer>(n)));

        return builder;
    }
}
