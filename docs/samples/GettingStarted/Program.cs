using Chronicles.Documents;
using Chronicles.Documents.Internal;
using Chronicles.EventStore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

using var host = Host.CreateDefaultBuilder()
  .ConfigureServices(services =>
  {
    services
      .AddChronicles(b => b
          //.Configure(o => // this will configure options
          //{
          //  o.UseDatabase("DropMe");
          //  o.UseCosmosEmulator();
          //  o.AddDocumentType<FirmwareDocument>("firmware");
          //  o.AddInitialization(o => o
          //      .CreateDatabase()
          //      .CreateContainer<FirmwareDocument>()
          //      .CreateEventStore());
          //})
          .AddSubscription<StreamEvent, EventSubscriptionSample2>(
              "my-subscription-01",
              o => o.PollingInterval = TimeSpan.FromSeconds(5))
          .AddEventStore(b => b
              .Configure(o =>
              {
                o.AddEvent<FirmwareChangeDetected>("firmware-change-detected:v1");
              })
              .AddEventSubscription(
                  "my-projection",
                  c => c
                    .Configure(o => o.Strategy = EventPartitioningStrategy.StreamId)
                    .AddEventConsumer<EventSubscriptionSample2>())))
      .AddDocumentStore("Firmware", b => b
          .Configure(o =>
            {
              o.UseDatabase("Firmware");
              o.UseCosmosEmulator();
              o.UseSubscriptionContainer("subscriptions");
            })
          .AddSubscription<FirmwareDocument, FirmwareDocumentSubscription>("read-model")
          //.AddSubscription<StreamEvent, EventSubscriptionSample>("sample10", o => o.StoreName = "Firmware")
          );

    services.ConfigureOptions<ConfigureDocumentStoreDropMe>();
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

//var client = host.Services.GetRequiredService<IEventStoreClient>();
//await client.WriteStreamAsync(
//  "firmware-update.123",
//  new[] { new FirmwareChangeDetected("1", "2", "3", "4", DateTimeOffset.UtcNow) });

await Task.Delay(5000);

//await foreach (var streamEvent in client.ReadStreamAsync("firmware-update.123"))
//{
//  Console.WriteLine($"{streamEvent.Metadata.Name}");
//}

await subscriptions.StopAsync(CancellationToken.None);

Console.WriteLine("Completed");
//await host.RunAsync();

//public class DeploymentConsumer
//{
//  public void ConsumeEvent(
//    IEventProcessorContext context,
//    FirmwareChangeDetected evt)
//  {

//  }
//}

[ContainerName("firmware")]
public record FirmwareDocument(
  string Id,
  string Pk,
  string Name,
  string Value) : IDocument
{
  public string GetDocumentId() => Id;

  public string GetPartitionKey() => Pk;
}

public record Unknown(string Text);

public record FirmwareUpdated(
  string ChargePointId,
  string ChargePointVendor,
  string ChargePointModel,
  string FirmwareVersion,
  DateTimeOffset OcppTimestamp);

public record FirmwareChangeDetected(
  string ChargePointId,
  string ChargePointVendor,
  string ChargePointModel,
  string FirmwareVersion,
  DateTimeOffset OcppTimestamp);

public class FirmwareDocumentSubscription
  : IDocumentProcessor<FirmwareDocument>
{
  public Task ErrorAsync(
    string leaseToken,
    Exception exception)
    => Task.CompletedTask;

  public Task ProcessAsync(
    IReadOnlyCollection<FirmwareDocument> changes,
    CancellationToken cancellationToken)
  {
    foreach (var doc in changes)
    {
      Console.WriteLine($"Firmware: {doc}");
    }

    return Task.CompletedTask;
  }
}

public class EventSubscriptionSample2
  : IDocumentProcessor<StreamEvent>
{
  public Task ErrorAsync(
    string leaseToken,
    Exception exception)
    => Task.CompletedTask;

  public Task ProcessAsync(
    IReadOnlyCollection<StreamEvent> changes,
    CancellationToken cancellationToken)
  {
    foreach (var streamEvent in changes)
    {
      Console.WriteLine($"DropMe: {streamEvent.Metadata.Name}:{streamEvent.Metadata.Version}");
    }

    return Task.CompletedTask;
  }
}

public class ConfigureDocumentStoreDropMe
  : IConfigureNamedOptions<DocumentOptions>
{
  public void Configure(DocumentOptions options)
    => Configure(Options.DefaultName, options);

  public void Configure(string? name, DocumentOptions options)
  {
    if (name == null || name == DocumentOptions.DefaultStoreName)
    {
      options.UseDatabase("DropMe");
      options.UseCosmosEmulator();
      options.AddDocumentType<FirmwareDocument>("firmware");
      options.AddInitialization(o => o
        .CreateDatabase()
        .CreateContainer<FirmwareDocument>()
        .CreateEventStore());
    }
  }
}