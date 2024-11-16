using Aspire.Hosting.Azure;

namespace Chronicles.AppHost;

public static class Extensions
{
    public static IResourceBuilder<AzureCosmosDBEmulatorResource> WithDefaultEndpoints(
        this IResourceBuilder<AzureCosmosDBEmulatorResource> builder)
        => builder.WithEndpoint("emulator", c =>
        {
            c.UriScheme = "https";
            c.Port = 8081;
        });
}
