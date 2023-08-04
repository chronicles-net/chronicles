using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

using var host = Host.CreateDefaultBuilder()
  .ConfigureServices(services =>
  {
    services
      .AddChronicles()
      .AddDocumentStore("event-store");
    //.AddEventStore();
  })
  .Build();

await host.RunAsync();
