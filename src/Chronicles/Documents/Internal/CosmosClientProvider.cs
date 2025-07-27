using System.Collections.Concurrent;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Options;

namespace Chronicles.Documents.Internal;

internal sealed class CosmosClientProvider : IDisposable, ICosmosClientProvider
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

        return options switch
        {
            { AccountEndpoint: { } ep, Credential: { } cred }
                => new CosmosClient(
                    ep,
                    cred,
                    options.CosmosClient),

            { AccountEndpoint: { } ep, AccountKey: { } key }
               => new CosmosClient(
                    ep,
                    key,
                    options.CosmosClient),

            { ConnectionString: { } cs }
                => new CosmosClient(cs, options.CosmosClient),

            _ => throw new InvalidOperationException(
                storeName != null
                ? $"Missing configuration for Cosmos connection ({storeName})"
                : $"Missing configuration for Cosmos connection")
        };
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
        => options switch
        {
            { DatabaseName: null } => false,
            { AccountEndpoint.Length: > 0, Credential: { } } => true,
            { AccountEndpoint.Length: > 0, AccountKey.Length: > 0 } => true,
            { ConnectionString.Length: > 0 } => true,
            _ => false,
        };
}
