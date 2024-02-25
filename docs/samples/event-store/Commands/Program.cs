using Chronicles.Documents.Internal;
using Chronicles.EventStore;
using Chronicles.EventStore.Internal.Commands;
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
                    .AddEvent<QuestEvents.QuestStarted>("quest-started:v1")
                    .AddEvent<QuestEvents.MembersJoined>("members-joined:v1"))));

      services // register command handlers
        .AddTransient<ICommandHandler<StartQuest.Command>, StartQuest.Handler>()
        .AddTransient<ICommandHandler<JoinQuest.Command>, JoinQuest.Handler>();
  })
  .Build();

// initialize database and containers 
await host
  .Services
  .GetRequiredService<IDocumentStoreInitializer>()
  .InitializeAsync(CancellationToken.None);

var streamId = new QuestStreamId("5");

var response = await host.Services
    .GetRequiredService<ICommandProcessor<StartQuest.Command>>()
    .ExecuteAsync(
        new StartQuest.Command("My Quest"),
        streamId,
        storeName: null,
        cancellationToken: CancellationToken.None);

response = await host.Services
    .GetRequiredService<ICommandProcessor<JoinQuest.Command>>()
    .ExecuteAsync(
        new JoinQuest.Command(["Bob", "Pete", "Otto"]),
        streamId,
        storeName: null,
        cancellationToken: CancellationToken.None);

// Get hold of event store client to start writing events.
var client = host.Services.GetRequiredService<IEventStoreClient>();
// Read events from a stream
await foreach (var streamEvent in client.ReadStreamAsync(streamId))
{
    Console.WriteLine($"{streamEvent.Metadata.Name}");
}

Console.WriteLine("Completed");

