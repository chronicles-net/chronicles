using Chronicles.Documents.Internal;
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
                .MapEvent<QuestEvents.MembersJoined>("members-joined:v1")
                .AddEventSubscription("projections", c => c
                    .MapStream(QuestStreamId.CategoryName, s => s
                        .AddDocumentProjection<QuestDocument, QuestProjection>()))
                .UseCommands(c => c
                    .MapCommand<JoinQuest.Command, JoinQuest.Handler, QuestDocument>()
                    .MapCommand<StartQuest.Command, StartQuest.Handler>())));
  })
  .Build();

// initialize database and containers 
await host
  .Services
  .GetRequiredService<IDocumentStoreInitializer>()
  .InitializeAsync(CancellationToken.None);

host.Start();
////
//var subscriptions = host.Services.GetRequiredService<ISubscriptionManager>();
//await subscriptions.StartAsync(CancellationToken.None);

await Task.Delay(15000);
//var streamId = new QuestStreamId("1");

//var response = await host.Services
//    .GetRequiredService<ICommandProcessor<StartQuest.Command>>()
//    .ExecuteAsync(
//        streamId,
//        new StartQuest.Command("My Quest"),
//        requestOptions: null,
//        cancellationToken: CancellationToken.None);

//response = await host.Services
//    .GetRequiredService<ICommandProcessor<JoinQuest.Command>>()
//    .ExecuteAsync(
//        streamId,
//        new JoinQuest.Command(["Bob", "Pete", "Otto"]),
//        requestOptions: null,
//        cancellationToken: CancellationToken.None);

//// Get hold of event store client to start writing events.
//var client = host.Services.GetRequiredService<IEventStoreClient>();
//// Read events from a stream
//await foreach (var streamEvent in client.ReadStreamAsync(streamId))
//{
//    Console.WriteLine($"{streamEvent.Metadata.Name}");
//}

Console.WriteLine("Completed");

