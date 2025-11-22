using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Hosting;
using Scalar.AspNetCore;

namespace Chronicles.ServiceDefaults;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds OpenAPI to the application when in Development mode.
    /// </summary>
    /// <param name="builder"></param>
    /// <param name="title">Title of open api spec</param>
    /// <param name="version">optional version</param>
    public static WebApplicationBuilder AddOpenApi(
        this WebApplicationBuilder builder,
        string title,
        string version,
        string serverUri)
    {
        return builder;
    }

    /// <summary>
    /// Maps OpenAPI and Scalar API Reference UIs to the application
    /// when in Development mode.
    /// </summary>
    public static WebApplication MapOpenApiWithUI(
        this WebApplication app)
    {
        if (!app.Environment.IsDevelopment())
        {
            return app;
        }

        app.MapOpenApi();
        app.MapScalarApiReference(o =>
        {
            o.DocumentDownloadType = DocumentDownloadType.None;
            o.HideModels = true;
            o.EnabledTargets = [ScalarTarget.Shell];
            o.EnabledClients = [ScalarClient.Curl];
            o.Theme = ScalarTheme.Solarized;
            o.CustomCss = """
                :root {
                    --scalar-sidebar-width: 350px;
                }
                """;
        });

        return app;
    }
}
