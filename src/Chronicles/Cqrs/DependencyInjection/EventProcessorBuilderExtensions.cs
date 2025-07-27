using Chronicles.Cqrs.Internal;
using Chronicles.Cqrs.Internal.EventProcessors;
using Chronicles.Documents;
using Chronicles.EventStore;
using Chronicles.EventStore.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Chronicles.Cqrs.DependencyInjection;

/// <summary>
/// Provides extension methods for configuring event processors in the Chronicles CQRS pipeline.
/// Use these methods to register projections, consumers, and processors for handling events and building read models.
/// </summary>
public static class EventProcessorBuilderExtensions
{
    /// <summary>
    /// Registers a document projection processor for the specified document and consumer types.
    /// Use this method to configure event processing that builds and maintains document-based read models from event streams.
    /// </summary>
    /// <typeparam name="TDocument">The type of the document being projected.</typeparam>
    /// <typeparam name="TConsumer">The type of the document projection consumer.</typeparam>
    /// <param name="builder">The event processor builder to configure.</param>
    /// <returns>The updated <see cref="EventProcessorBuilder"/> instance.</returns>
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

    /// <summary>
    /// Registers a state projection processor for the specified state and consumer types.
    /// Use this method to configure event processing that builds and maintains state-based read models from event streams.
    /// </summary>
    /// <typeparam name="TState">The type of the state being projected.</typeparam>
    /// <typeparam name="TConsumer">The type of the state projection consumer.</typeparam>
    /// <param name="builder">The event processor builder to configure.</param>
    /// <returns>The updated <see cref="EventProcessorBuilder"/> instance.</returns>
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

    /// <summary>
    /// Registers a state consumer processor for the specified state and consumer types.
    /// Use this method to configure event processing that reacts to events and updates state, typically for side effects or notifications.
    /// </summary>
    /// <typeparam name="TState">The type of the state being consumed.</typeparam>
    /// <typeparam name="TConsumer">The type of the state consumer.</typeparam>
    /// <param name="builder">The event processor builder to configure.</param>
    /// <returns>The updated <see cref="EventProcessorBuilder"/> instance.</returns>
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

    /// <summary>
    /// Registers a publishing document projection processor for the specified document, consumer, and publisher types.
    /// Use this method to configure event processing that builds document-based read models and publishes them to external systems after processing.
    /// </summary>
    /// <typeparam name="TDocument">The type of the document being projected.</typeparam>
    /// <typeparam name="TConsumer">The type of the document projection consumer.</typeparam>
    /// <typeparam name="TPublisher">The type of the document publisher.</typeparam>
    /// <param name="builder">The event processor builder to configure.</param>
    /// <returns>The updated <see cref="EventProcessorBuilder"/> instance.</returns>
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
