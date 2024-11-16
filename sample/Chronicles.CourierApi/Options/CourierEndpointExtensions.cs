using Chronicles.CourierApi.Couriers;
using Chronicles.Cqrs;
using Chronicles.Documents;

namespace Chronicles.CourierApi.Options;

public static partial class CourierEndpointExtensions
{
    public static IEndpointRouteBuilder MapCourierEndpoints(
        this IEndpointRouteBuilder app,
        string routePrefix)
    {
        var group = app.MapGroup(routePrefix + "/couriers")
            .WithTags("Courier")
            .AllowAnonymous();

        group
            .MapPost("/", static async (RegisterCourier.Command request, ICommandProcessor<RegisterCourier.Command> processor, CancellationToken cancellationToken)
            => await processor.ExecuteAsync(
                    CourierStreamId.Create(),
                    request,
                    requestOptions: null,
                    cancellationToken) switch
                {
                    { Result: EventStore.ResultType.NotModified } => Results.StatusCode(StatusCodes.Status304NotModified),
                    { Result: EventStore.ResultType.Conflict } => Results.Conflict(),
                    { Response: { } response } => Results.Ok(response),
                    { } => Results.Ok(),
                    _ => Results.InternalServerError(),
                })
            .WithSummary("Register courier");

        group
            .MapGet("/{courierId}", static async (string courierId, IDocumentReader<CourierDocument> reader, CancellationToken cancellationToken)
            => await reader.FindAsync(
                courierId,
                courierId,
                cancellationToken) switch
            {
                { } doc => Results.Ok(doc),
                _ => Results.NotFound(),
            })
            .WithSummary("Get courier");

        group
            .MapGet("/", static async (IDocumentReader<CourierDocument> reader, CancellationToken cancellationToken)
            => await reader
                .PagedQueryAsync(d => d, partitionKey: null, maxItemCount: 100, cancellationToken: cancellationToken) switch
            {
                { } doc => Results.Ok(doc),
                _ => Results.NotFound(),
            })
            .WithSummary("List all couriers");

        return app;
    }
}
