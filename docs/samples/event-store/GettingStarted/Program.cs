using Chronicles.Documents;
using Chronicles.Documents.Internal;
using Chronicles.EventStore;
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
                  .CreateContainer<QuestDocument>()
                  .CreateEventStore()))
            .AddEventStore(b => b
                .Configure(o => o
                    .AddEvent<QuestStarted>("quest-started:v1"))));
  })
  .Build();

// initialize database and containers 
await host
  .Services
  .GetRequiredService<IDocumentStoreInitializer>()
  .InitializeAsync(CancellationToken.None);

// Get hold of event store client to start writing events.
var client = host.Services.GetRequiredService<IEventStoreClient>();
var streamId = new QuestStreamId("1");
await client.WriteStreamAsync(
  streamId,
  new[] { new QuestStarted("Awesome Quest") });

// Read events from a stream
await foreach (var streamEvent in client.ReadStreamAsync(streamId))
{
    Console.WriteLine($"{streamEvent.Metadata.Name}");
}

Console.WriteLine("Completed");

public record QuestStreamId(
    string Id) : StreamId("quest", Id);

[ContainerName("quest")]
public record QuestDocument(
  string Id,
  string Pk,
  string Name,
  IReadOnlyCollection<string> Members) : IDocument
{
    public string GetDocumentId() => Id;

    public string GetPartitionKey() => Pk;
}

public record QuestStarted(
  string Name);
