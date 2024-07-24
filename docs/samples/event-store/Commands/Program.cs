using Chronicles.Cqrs;
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
                .MapEvent<QuestEvents.MembersJoined>("members-joined:v1")
                .UseCommands(c => c
                    .MapCommand<StartQuest.Command, StartQuest.Handler>(new CommandOptions() { Consistency = CommandConsistency.ReadWrite })
                    .MapCommand<JoinQuest.Command, JoinQuest.Handler, QuestDocument>())));
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
        streamId,
        new StartQuest.Command("My Quest"),
        requestOptions: null,
        cancellationToken: CancellationToken.None);

response = await host.Services
    .GetRequiredService<ICommandProcessor<JoinQuest.Command>>()
    .ExecuteAsync(
        streamId,
        new JoinQuest.Command(["Bob", "Pete", "Otto"]),
        requestOptions: null,
        cancellationToken: CancellationToken.None);

// Read events from a stream
var reader = host.Services.GetRequiredService<IEventStreamReader>();
await foreach (var streamEvent in reader.ReadAsync(streamId))
{
    Console.WriteLine($"{streamEvent.Metadata.Name}");
}

Console.WriteLine("Completed");

