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
                .MapEvent<QuestEvents.QuestStarted>("quest-started:v1")));
  })
  .Build();

// initialize database and containers 
await host
  .Services
  .GetRequiredService<IDocumentStoreInitializer>()
  .InitializeAsync(CancellationToken.None);

// Get hold of event store client to start writing events.
var reader = host.Services.GetRequiredService<IEventStreamReader>();
var writer = host.Services.GetRequiredService<IEventStreamWriter>();
var streamId = new QuestStreamId("100");
await writer.WriteAsync(
  streamId,
  [new QuestEvents.QuestStarted("Awesome Quest")]);

// Read events from a stream
await foreach (var streamEvent in reader.ReadAsync(streamId))
{
    Console.WriteLine($"{streamEvent.Metadata.Name}");
}

Console.WriteLine("Completed");

