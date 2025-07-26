using Chronicles.CourierApi.Couriers;
using Chronicles.CourierApi.Options;
using Chronicles.CourierApi.Shipments;
using Chronicles.Cqrs.DependencyInjection;
using Chronicles.EventStore.DependencyInjection;
using Chronicles.ServiceDefaults;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddOpenApi();
builder.AddOpenApi("Courier API", "v1", "https://localhost:7084");

builder.Services.ConfigureOptions<ConfigureDocumentOptions>();
builder.Services.AddChronicles(b => b
    .WithEventStore(evtStore => evtStore
        .AddShipmentEvents()
        .AddCourierEvents()
        .AddEventSubscription(
            "courier-projections",
            s => s
                .MapStream(CourierStreamId.CategoryName, cb => cb
                    .AddDocumentProjection<CourierDocument, CourierProjection>())
                .MapStream(ShipmentStreamId.CategoryName, cb => cb
                    .AddDocumentProjection<ShipmentDocument, ShipmentProjection>())
                )
        .WithCqrs(cqrs => cqrs
            .AddCourierCommands()
            .AddShipmentCommands())));

var app = builder.Build();

app.MapOpenApiWithUI();

app.UseHttpsRedirection();

app.MapCourierEndpoints("v1");

app.Run();
