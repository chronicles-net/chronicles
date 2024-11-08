using System.Collections.Concurrent;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Options;

namespace Chronicles.Documents.Internal;

public sealed class CosmosClientProvider : IDisposable, ICosmosClientProvider
{
    private readonly ConcurrentDictionary<string, CosmosClient> clients = new();
    private readonly IOptionsMonitor<DocumentOptions> documentOptions;

    public CosmosClientProvider(
        IOptionsMonitor<DocumentOptions> documentOptions)
        => this.documentOptions = documentOptions;

    public CosmosClient GetClient(
        string? storeName = null)
        => clients
            .GetOrAdd(
                storeName ?? Options.DefaultName,
                _ => CreateClient(storeName));

    public void Dispose()
    {
        foreach (var client in clients.Values)
        {
            client.Dispose();
        }
    }

    private CosmosClient CreateClient(
        string? storeName = null)
    {
        var options = GetOptions(storeName);
        if (options.AllowAnyServerCertificate)
        {
            options.CosmosClient.ServerCertificateCustomValidationCallback = (_, _, _) => true;
        }

        return options.Credential is not null
            ? new CosmosClient(
                options.AccountEndpoint,
                options.Credential,
                options.CosmosClient)
            : new CosmosClient(
                $"AccountEndpoint={options.AccountEndpoint};" +
                $"AccountKey={options.AccountKey}",
                options.CosmosClient);
    }

    private DocumentOptions GetOptions(string? storeName)
    {
        var options = documentOptions.Get(storeName);
        if (!IsValid(options))
        {
            var name = string.IsNullOrEmpty(storeName) ? "default" : $"\"{storeName}\"";
            throw new InvalidOperationException(
                $"The {nameof(DocumentOptions)} for {name} document store is not correctly configured.");
        }

        return options;
    }

    private static bool IsValid(DocumentOptions? options)
        => options is not null
        && !string.IsNullOrEmpty(options.AccountEndpoint)
        && (!string.IsNullOrEmpty(options.AccountKey) || options.Credential is not null)
        && !string.IsNullOrEmpty(options.DatabaseName);
}
