using Chronicles.Documents;
using Chronicles.Documents.Internal;
using Chronicles.EventStore;
using Chronicles.EventStore.Samples;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

using var host = Host.CreateDefaultBuilder()
  .ConfigureServices(services =>
  {
      services
        .AddChronicles(b => b
            .Configure(o => o
                .UseDatabase("QuestStore")
                .UseCosmosEmulator()
                .AddInitialization(o => o
                  .CreateDatabase()
                  .CreateContainer<QuestDocument>(p => p.PartitionKeyPath = "/pk")
                  .CreateEventStore()))
            .AddEventStore(b => b
                .Configure(o => o
                    .AddEvent<QuestEvents.QuestStarted>("quest-started:v1"))
                .AddEventSubscription(
                  "quest-projection",
                  c => c
                    .Configure(o =>
                    {
                        o.Strategy = EventPartitioningStrategy.StreamId;
                        //o.Filter = e => e.Metadata.StreamId.Category.StartsWith("firmware-update");
                    })
                    .AddEventConsumer<EventConsumerSample>()
                    .AddEventProjection<QuestDocument, QuestProjection>())));
      //.AddDocumentStore("Firmware", b => b
      //    .Configure(o =>
      //      {
      //        o.UseDatabase("Firmware");
      //        o.UseCosmosEmulator();
      //        o.UseSubscriptionContainer("subscriptions");
      //      })
      //    .AddSubscription<FirmwareDocument, FirmwareDocumentSubscription>("read-model")
      //    //.AddSubscription<StreamEvent, EventSubscriptionSample>("sample10", o => o.StoreName = "Firmware")
      //    );

      //services.ConfigureOptions<ConfigureDocumentStoreDropMe>();
      //.AddAllStreamSubscription(
      //  "my",
      //  c => c.WithFilter("deployments.*")
      //       c.WithBatchSize(30)
      //       c.WithProcessingStrategy(EventProcessingStrategy.Single | Parallel)
      //       c.WithPartitioningStrategy(PartitioningStrategy.Stream | Event)
      //       c.AddEventConsumer<EventSubscriptionSample>()
      //.AddStreamSubscription(
      //  "my-subscription", // <- should we only allow stream filter for the subscription or is it also useful on the processors
      //  c => c.AddEventConsumer<EventSubscriptionSample>()
      //        .AddEventConsumer<MyProjection>()
      //        .WithFilter("deployments.*")
      //        .WithBatchSize(30)
      //        .WithPartitioningByStream() // instruct the subscription to group event batch by stream id
      //        .WithEventPartitioning(Stream | Event)
      //        .WithParallelEventProcessing(5)
      //        .WithSingleEventProcessing()
      //        );
  })
  .Build();

// initialize database and containers 
await host
  .Services
  .GetRequiredService<IDocumentStoreInitializer>()
  .InitializeAsync(CancellationToken.None);

var subscriptions = host.Services.GetRequiredService<ISubscriptionManager>();
await subscriptions.StartAsync(CancellationToken.None);

//await Task.Delay(2000);

var client = host.Services.GetRequiredService<IEventStoreClient>();
await client.WriteStreamAsync(
  new QuestStreamId("1"),
  new[] { new QuestEvents.QuestStarted("Awesome Quest") });

await Task.Delay(10000);

//await foreach (var streamEvent in client.ReadStreamAsync("firmware-update.123"))
//{
//  Console.WriteLine($"{streamEvent.Metadata.Name}");
//}

await subscriptions.StopAsync(CancellationToken.None);

Console.WriteLine("Completed");
//await host.RunAsync();


public class QuestProjection :
    IDocumentProjection<QuestDocument>,
    IConsumeEvent<QuestEvents.QuestStarted>
{
    private QuestDocument document = new()
    {
        Id = string.Empty,
        Pk = string.Empty,
        Name = string.Empty,
    };

    public void Consume(
        QuestEvents.QuestStarted evt,
        EventMetadata metadata)
    {
        document.Name = evt.Name;
    }

    public async Task CommitAsync(
        ProjectionKind kind,
        IDocumentWriter<QuestDocument> writer,
        CancellationToken cancellationToken)
        => await writer
            .WriteAsync(document, cancellationToken)
            .ConfigureAwait(false);

    public async Task ResumeAsync(
        ProjectionKind kind,
        IDocumentReader<QuestDocument> reader,
        StreamId streamId,
        StreamEvent[] events,
        CancellationToken cancellationToken)
        => document = await reader
            .FindAsync(
                documentId: streamId.Id,
                GetPartitionKey(),
                cancellationToken)
            .ConfigureAwait(false)
        ?? new()
        {
            Id = streamId.Id,
            Pk = GetPartitionKey(),
            Name = string.Empty,
        };

    private static string GetPartitionKey() => "MyPartition";
}
/*
 *   CreateAsync(state)
 * else
 *   Create(state)
 * 
 * ConsumeEvent(evt)
 * ConsumeEvent(evt, state)
 * ConsumeEventAsync(evt)
 * ConsumeEventAsync(evt, state)
 * 
 * ConsumeAnyEvent(evt)
 * ConsumeAnyEvent(evt, state)
 * ConsumeAnyEventAsync(evt)
 * ConsumeAnyEventAsync(evt, state)
 *
 * ConsumeGroupedEvents(evt)
 * ConsumeGroupedEventsAsync(evt)
 */

public class EventConsumerSample(
    IDocumentReader<QuestDocument> reader) :
    IConsumeEvent<QuestEvents.QuestStarted, QuestDocument>,
    IConsumeEventStateProviderAsync<QuestDocument>
{
    public QuestDocument Create(StreamEvent evt)
        => new()
        {
            Id = evt.Metadata.StreamId.Id,
            Pk = GetPartitionKey(),
            Name = string.Empty,
        };

    public async Task<QuestDocument> CreateAsync(
        StreamEvent evt,
        CancellationToken cancellationToken)
        => await reader
            .FindAsync(
                documentId: evt.Metadata.StreamId.Id,
                GetPartitionKey(),
                cancellationToken)
            .ConfigureAwait(false)
        ?? Create(evt);

    public QuestDocument Consume(QuestEvents.QuestStarted evt, EventMetadata metadata, QuestDocument state)
    {
        Console.WriteLine($"{metadata.Name}:{metadata.Version}");
        Console.WriteLine($"{evt}");

        state.Name = evt.Name;
        return state;
    }

    private static string GetPartitionKey() => "demo";
}
