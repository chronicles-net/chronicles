using Chronicles.Documents.Serialization;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Options;

namespace Chronicles.Documents.Internal;

public sealed class CosmosClientProvider : IDisposable, ICosmosClientProvider
{
    private readonly IOptions<ChroniclesCosmosOptions> cosmosOptions;
    private readonly IOptions<CosmosClientOptions> cosmosClientOptions;
    private readonly IJsonCosmosSerializer serializer;
    private CosmosClient? client;

    public CosmosClientProvider(
        IOptions<ChroniclesCosmosOptions> cosmosOptions,
        IOptions<CosmosClientOptions> cosmosClientOptions,
        IJsonCosmosSerializer serializer)
    {
        this.cosmosOptions = cosmosOptions;
        this.cosmosClientOptions = cosmosClientOptions;
        this.serializer = serializer;

        var options = cosmosOptions.Value;
        if (!IsValid(options))
        {
            throw new InvalidOperationException(
                $"Invalid configuration in {nameof(ChroniclesCosmosOptions)}.");
        }
    }

    public CosmosClient GetClient()
        => client ??= CreateClient();

    public void Dispose()
    {
        client?.Dispose();
    }

    private static bool IsValid(ChroniclesCosmosOptions? options)
        => options is not null
        && !string.IsNullOrEmpty(options.AccountEndpoint)
        && (!string.IsNullOrEmpty(options.AccountKey) || options.Credential is not null)
        && !string.IsNullOrEmpty(options.DefaultDatabaseName);

    private CosmosClient CreateClient()
    {
        var options = CreateCosmosClientOptions();
        options.Serializer = cosmosClientOptions.Value.Serializer
            ?? new CosmosSerializerAdapter(serializer);

        return cosmosOptions.Value.Credential is not null
            ? new CosmosClient(
                cosmosOptions.Value.AccountEndpoint,
                cosmosOptions.Value.Credential,
                options)
            : new CosmosClient(
                $"AccountEndpoint={cosmosOptions.Value.AccountEndpoint};" +
                $"AccountKey={cosmosOptions.Value.AccountKey}",
                options);
    }

    private CosmosClientOptions CreateCosmosClientOptions()
    {
        var result = new CosmosClientOptions();

        if (cosmosClientOptions is { Value: { } o })
        {
            if (!string.IsNullOrEmpty(o.ApplicationName))
            {
                result.ApplicationName = o.ApplicationName;
            }

            result.ApplicationPreferredRegions = o.ApplicationPreferredRegions;
            result.ApplicationRegion = o.ApplicationRegion;
            result.ConnectionMode = o.ConnectionMode;
            result.ConsistencyLevel = o.ConsistencyLevel;

            foreach (var handler in o.CustomHandlers)
            {
                result.CustomHandlers.Add(handler);
            }

            result.HttpClientFactory = o.HttpClientFactory;
            result.IdleTcpConnectionTimeout = o.IdleTcpConnectionTimeout;
            result.LimitToEndpoint = o.LimitToEndpoint;
            result.MaxRequestsPerTcpConnection = o.MaxRequestsPerTcpConnection;
            result.MaxRetryWaitTimeOnRateLimitedRequests = o.MaxRetryWaitTimeOnRateLimitedRequests;
            result.MaxTcpConnectionsPerEndpoint = o.MaxTcpConnectionsPerEndpoint;
            result.OpenTcpConnectionTimeout = o.OpenTcpConnectionTimeout;
            result.PortReuseMode = o.PortReuseMode;
            result.RequestTimeout = o.RequestTimeout;
            result.SerializerOptions = o.SerializerOptions;
            result.WebProxy = o.WebProxy;
            result.EnableTcpConnectionEndpointRediscovery = o.EnableTcpConnectionEndpointRediscovery;
            result.GatewayModeMaxConnectionLimit = o.GatewayModeMaxConnectionLimit;
            result.MaxRetryAttemptsOnRateLimitedRequests = o.MaxRetryAttemptsOnRateLimitedRequests;
        }

        return result;
    }
}
