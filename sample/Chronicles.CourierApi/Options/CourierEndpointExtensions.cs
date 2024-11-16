using Chronicles.CourierApi.Couriers;
using Chronicles.Cqrs;

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
            .WithSummary(nameof(RegisterCourier));

        return app;
    }
}