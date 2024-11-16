using Chronicles.AppHost;

var builder = DistributedApplication.CreateBuilder(args);

var cosmos = builder
    .AddAzureCosmosDB("cosmosdb")
    .AddDatabase("CourierDB")
    .RunAsEmulator(c => c
        .WithLifetime(ContainerLifetime.Persistent)
        .WithDataVolume()
        .WithDefaultEndpoints());

var courier = builder
    .AddProject<Projects.Chronicles_CourierApi>("courier")
    .WaitFor(cosmos)
    .WithReference(cosmos);

var order = builder
    .AddProject<Projects.Chronicles_OrderApi>("order")
    .WaitFor(cosmos)
    .WithReference(cosmos);

var restaurant = builder
    .AddProject<Projects.Chronicles_RestaurantApi>("restaurant")
    .WaitFor(cosmos)
    .WithReference(cosmos);

builder.Build().Run();
