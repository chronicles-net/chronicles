using System.Collections.Concurrent;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Options;

namespace Chronicles.Documents.Internal;

public sealed class CosmosClientProvider : IDisposable, ICosmosClientProvider
{
    private readonly ConcurrentDictionary<string, CosmosClient> clients = new();
    private readonly ConcurrentDictionary<string, CosmosSerializer> serializers = new();
    private readonly IOptionsMonitor<DocumentOptions> documentOptions;

    public CosmosClientProvider(
        IOptionsMonitor<DocumentOptions> documentOptions)
    {
        this.documentOptions = documentOptions;

        var options = documentOptions.CurrentValue;
        if (!IsValid(options))
        {
            throw new InvalidOperationException(
                $"Invalid configuration in {nameof(DocumentOptions)}.");
        }
    }

    public CosmosClient GetClient(
        string? clientName = null)
        => clients
            .GetOrAdd(
                clientName ?? Options.DefaultName,
                CreateClient);

    public ICosmosSerializer GetSerializer(
        string? clientName = null)
        => GetSerializer(clientName, null);

    public void Dispose()
    {
        foreach (var client in clients.Values)
        {
            client.Dispose();
        }
    }

    private static bool IsValid(DocumentOptions? options)
        => options is not null
        && !string.IsNullOrEmpty(options.AccountEndpoint)
        && (!string.IsNullOrEmpty(options.AccountKey) || options.Credential is not null)
        && !string.IsNullOrEmpty(options.DatabaseName);

    private CosmosClient CreateClient(
        string? clientName = null)
    {
        var options = documentOptions.Get(clientName);
        options.CosmosClientOptions.Serializer = new CosmosSerializerAdapter(
            GetSerializer(clientName, options));

        return options.Credential is not null
            ? new CosmosClient(
                options.AccountEndpoint,
                options.Credential,
                options.CosmosClientOptions)
            : new CosmosClient(
                $"AccountEndpoint={options.AccountEndpoint};" +
                $"AccountKey={options.AccountKey}",
                options.CosmosClientOptions);
    }

    private ICosmosSerializer GetSerializer(
        string? clientName,
        DocumentOptions? options)
        => serializers
            .GetOrAdd(
                clientName ?? Options.DefaultName,
                n => new CosmosSerializer(
                    (options ?? documentOptions.Get(n)).SerializerOptions));
}
