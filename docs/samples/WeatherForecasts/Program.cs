using System.Text.Json.Serialization;
using Chronicles.Documents;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Cosmos;

var builder = WebApplication.CreateBuilder(args);

// Add chronicles to the container.
builder.Services
    .AddChronicles(o => o
        .UseDatabase("WeatherForecasts")
        .AddDocumentType<WeatherForecast>("forecasts")
        .UseCosmosEmulator()
        .AddInitialization(i => i
            .CreateDatabase(ThroughputProperties.CreateManualThroughput(400))
            .CreateContainer(new ContainerProperties
            {
                Id = "forecasts",
                PartitionKeyPath = "/id",
                IndexingPolicy = new()
                {
                    Automatic = true,
                    IndexingMode = IndexingMode.Consistent,
                    IncludedPaths = { new() { Path = "/*" } },
                },
            })));

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app
    .MapGet(
        "/weatherforecast",
        (IDocumentReader<WeatherForecast> reader)
            => reader.ReadAllAsync(partitionKey: null))
    .WithName("GetWeatherForecast")
    .WithOpenApi();

app
    .MapPost(
        "/waetherforecasts",
        ([FromBody] WeatherForecast body, IDocumentWriter<WeatherForecast> writer)
            => writer.WriteAsync(body))
    .WithName("PostWeatherForecast")
    .WithOpenApi();

app.Run();

internal record WeatherForecast(
    [property: JsonPropertyName("id")] string Id,
    DateOnly Date,
    int TemperatureC,
    string? Summary) : IDocument
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);

    public string GetDocumentId() => Id;

    public string GetPartitionKey() => Id;
}
