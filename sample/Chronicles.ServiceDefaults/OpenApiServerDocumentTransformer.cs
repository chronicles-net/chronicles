using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace Chronicles.ServiceDefaults;

public class OpenApiServerDocumentTransformer(
    string title,
    string version,
    string serverUri)
    : IOpenApiDocumentTransformer
{
    public Task TransformAsync(
        OpenApiDocument document,
        OpenApiDocumentTransformerContext context,
        CancellationToken cancellationToken)
    {
        document.Info.Title = title;
        document.Info.Version = version;

        document.Servers?.Clear();
        document.Servers?.Add(
            new OpenApiServer
            {
                Url = serverUri,
            });

        return Task.CompletedTask;
    }
}
