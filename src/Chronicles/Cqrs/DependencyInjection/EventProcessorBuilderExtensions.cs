using Chronicles.Cqrs.Internal;
using Chronicles.Cqrs.Internal.EventProcessors;
using Chronicles.Documents;
using Chronicles.EventStore;
using Chronicles.EventStore.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Chronicles.Cqrs.DependencyInjection;

public static class EventProcessorBuilderExtensions
{
    public static EventProcessorBuilder AddDocumentProjection<TDocument, TConsumer>(
        this EventProcessorBuilder builder)
        where TDocument : class, IDocument
        where TConsumer : class, IDocumentProjection<TDocument>
    {
        builder.Services.AddKeyedSingleton<TConsumer>(builder.Name);
        builder.Services.AddKeyedSingleton(builder.Name, (s, n) =>
            new DocumentProjectionProcessor<TConsumer, TDocument>(
                s.GetRequiredKeyedService<TConsumer>(n),
                s.GetRequiredService<IDocumentReader<TDocument>>(),
                s.GetRequiredService<IDocumentWriter<TDocument>>()));
        builder.Services.AddKeyedSingleton<IEventProcessor>(builder.Name, (s, n) =>
            s.GetRequiredKeyedService<DocumentProjectionProcessor<TConsumer, TDocument>>(n));
        builder.Services.AddKeyedSingleton<IDocumentProjectionRebuilder<TConsumer, TDocument>>(builder.StoreName, (s, n) =>
            new DocumentProjectionRebuilder<TConsumer, TDocument>(
                builder.StoreName,
                s.GetRequiredKeyedService<TConsumer>(builder.Name),
                s.GetRequiredKeyedService<DocumentProjectionProcessor<TConsumer, TDocument>>(builder.Name),
                s.GetRequiredService<IEventStreamReader>()));
        builder.Services.TryAddSingleton(s =>
            s.GetRequiredKeyedService<IDocumentProjectionRebuilder<TConsumer, TDocument>>(string.Empty));

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

    public static EventProcessorBuilder AddPublishingDocumentProjection<TDocument, TConsumer, TPublisher>(
        this EventProcessorBuilder builder)
        where TDocument : class, IDocument
        where TConsumer : class, IDocumentProjection<TDocument>
        where TPublisher : class, IDocumentPublisher<TDocument>
    {
        builder.Services.AddKeyedSingleton<TConsumer>(builder.Name);
        builder.Services.AddKeyedSingleton<TPublisher>(builder.Name);
        builder.Services.AddKeyedSingleton(builder.Name, (s, n) =>
            new PublishDocumentProjectionProcessor<TConsumer, TDocument, TPublisher>(
                s.GetRequiredKeyedService<TConsumer>(n),
                s.GetRequiredService<IDocumentReader<TDocument>>(),
                s.GetRequiredService<IDocumentWriter<TDocument>>(),
                s.GetRequiredKeyedService<TPublisher>(n)));
        builder.Services.AddKeyedSingleton<IEventProcessor>(builder.Name, (s, n) =>
            s.GetRequiredKeyedService<PublishDocumentProjectionProcessor<TConsumer, TDocument, TPublisher>>(n));
        builder.Services.AddKeyedSingleton<IDocumentProjectionRebuilder<TConsumer, TDocument>>(builder.StoreName, (s, n) =>
            new DocumentProjectionRebuilder<TConsumer, TDocument>(
                builder.StoreName,
                s.GetRequiredKeyedService<TConsumer>(builder.Name),
                s.GetRequiredKeyedService<PublishDocumentProjectionProcessor<TConsumer, TDocument, TPublisher>>(builder.Name),
                s.GetRequiredService<IEventStreamReader>()));
        builder.Services.TryAddSingleton(s =>
            s.GetRequiredKeyedService<IDocumentProjectionRebuilder<TConsumer, TDocument>>(string.Empty));

        return builder;
    }
}
