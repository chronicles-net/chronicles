using Chronicles.Cqrs;
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
                .MapEvent<QuestEvents.QuestStarted>("quest-started:v1")
                .AddEventSubscription(
                  "quest-projection",
                  o =>
                  {
                      o.SubscriptionOptions.BatchSize = 10;
                  },
                  c => c
                    .MapStream("quest", stream => stream
                        .AddDocumentProjection<QuestDocument, QuestProjection>()))));

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


      // .MapCommand<JoinQuest.Command, JoinQuest.Handler>(c => c
      //    .Configure(o =>
      //    { // Default command options, can be overridden when executing the command
      //      o.RequiredState = StreamState.Active;
      //      o.Behavior = OnConflict.RerunCommand;
      //      o.BehaviorCount = 3;
      //    })
      //    .AddStateProjection<QuestDocument, QuestProjections>()
      //    .AddSnapshotProvider<T>())


      //.AddStreamSubscription(
      //  "my-subscription",
      //  c => c
      //    .MapStream("locations", stream => stream // every map-xx will run in parallel
      //        .AddStateProvider<DocumentStateProvider, TDocument>(name: "quest-document")
      //        .AddDocumentProjection<TDocument, TEventConsumer>(
      //            name: "quest-projection",
      //            partitionKeySelector: static streamId => "active",
      //            idSelector: static streamId => streamId.Id)
      //        .AddEventConsumer<MyStateProjection, MyStateProvider, TDocument>(name: "my-state") 
      //        .AddEventConsumer<MyStateProjection2>()
      //        .AddFailureHandler<T>((exception, event, object consumer) => {})
      //    .MapAllStreams(s => s
      //        .UseResumeReadModel<MyReadModelDocument>()
      //
      // Find or Create New Document UseDocumentProjection<>()
      //   Project event to consumer
      // Save document    
      //
      // Find or Create New Document
      //   Project event to consumer
      //     publish event to service bus
      // Save document    
      //
      //
      //
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

var writer = host.Services.GetRequiredService<IEventStreamWriter>();
await writer.WriteAsync(
  new QuestStreamId("1"),
  [new QuestEvents.QuestStarted("Awesome Quest")]);

await Task.Delay(10000);

//await foreach (var streamEvent in client.ReadStreamAsync("firmware-update.123"))
//{
//  Console.WriteLine($"{streamEvent.Metadata.Name}");
//}

await subscriptions.StopAsync(CancellationToken.None);

Console.WriteLine("Completed");
//await host.RunAsync();
public class QuestProjection
    : IDocumentProjection<QuestDocument>
{
    public QuestDocument CreateState(StreamId streamId)
        => new()
        {
            Id = streamId.Id,
            Pk = "MyPartition",
            Name = string.Empty,
        };

    public QuestDocument ConsumeEvent(
        StreamEvent evt,
        QuestDocument state)
    {
        Console.WriteLine($"{evt.Metadata.Name}:{evt.Metadata.Version}");
        Console.WriteLine($"{evt}");

        if (evt.Data is QuestEvents.QuestStarted e)
        {
            state.Name = e.Name;
        }

        return state;
    }
}