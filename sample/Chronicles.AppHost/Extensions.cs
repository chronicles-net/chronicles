using Aspire.Hosting.Azure;

namespace Chronicles.AppHost;

static class Extensions
{
    public static ReferenceExpression GetOcppEndpoint(
        this IResourceBuilder<ProjectResource> project)
    {
        var ep = project.GetEndpoint("https");
        var host = ep.Property(EndpointProperty.Host);
        var port = ep.Property(EndpointProperty.Port);

        return ReferenceExpression.Create($"wss://{host}:{port}/ocpp");
    }

    public static IResourceBuilder<AzureCosmosDBEmulatorResource> WithDefaultEndpoints(
        this IResourceBuilder<AzureCosmosDBEmulatorResource> builder)
        => builder.WithEndpoint("emulator", c =>
        {
            c.UriScheme = "https";
            c.Port = 8081;
        });
}
