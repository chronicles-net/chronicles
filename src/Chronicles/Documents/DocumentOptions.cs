using System.Text.Json;
using Azure.Core;
using Microsoft.Azure.Cosmos;

namespace Chronicles.Documents;

/// <summary>
/// Options for configuring the connection to Cosmos.
/// </summary>
public class DocumentOptions
{
    public const string EmulatorEndpoint = "https://localhost:8081/";
    public const string EmulatorAuthKey = "C2y6yDjf5/R+ob0N8A7Cgv30VRDJIWEHLM+4QDU5DE2nQ9nDuVTqobD4b8mGGyPMbIZnqyMsEcaGQy67XIw/Jw==";
    private readonly Dictionary<Type, string> containerNames = new();

    /// <summary>
    /// Gets the Cosmos account endpoint URI.
    /// </summary>
    /// <remarks>
    /// You can get this value from the Azure portal.
    /// Navigate to your Azure Cosmos account.
    /// Open the Overview pane and copy the URI value.
    /// </remarks>
    public string AccountEndpoint { get; private set; } = default!;

    /// <summary>
    /// Gets the Cosmos account key.
    /// </summary>
    /// <remarks>
    /// You can get this value from the Azure portal.
    /// Navigate to your Azure Cosmos account.
    /// Open the Connection Strings or Keys pane, and copy the
    /// "Password" or PRIMARY KEY value.
    /// The <see cref="AccountKey"/> is required if <see cref="Credential"/> is not specified.
    /// </remarks>
    public string? AccountKey { get; private set; } = default!;

    /// <summary>
    /// Gets the TokenCredential to use instead of <see cref="AccountKey"/>.
    /// </summary>
    /// <remarks>
    /// When <see cref="TokenCredential"/> is provided the property <see cref="AccountKey"/> is not needed.
    /// </remarks>
    public TokenCredential? Credential { get; private set; }

    /// <summary>
    /// Gets a value indicating if any server certificate is allowed when using the CosmosEmulator.
    /// </summary>
    public bool AllowAnyServerCertificate { get; private set; }

    /// <summary>
    /// Gets or sets the options for controlling the json serializer.
    /// </summary>
    public JsonSerializerOptions SerializerOptions { get; set; }
        = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    public string DatabaseName { get; set; } = "Chronicles";

    public string SubscriptionContainerName { get; set; } = "Subscriptions";

    public CosmosClientOptions CosmosClientOptions { get; } = new();

    public IReadOnlyDictionary<Type, string> ContainerNames => containerNames;

    /// <summary>
    /// Configure event store to use <seealso cref="TokenCredential"/>.
    /// </summary>
    /// <param name="endpoint">Cosmos account endpoint.</param>
    /// <param name="credentials">Token credentials to use when connecting to cosmos.</param>
    /// <exception cref="ArgumentNullException">Throws when <paramref name="credentials"/> or <paramref name="endpoint"/> are null.</exception>
    /// <returns>The updated <see cref="DocumentOptions"/>.</returns>
    public DocumentOptions UseCredentials(
        string endpoint,
        TokenCredential credentials)
    {
        Credential = credentials ?? throw new ArgumentNullException(nameof(credentials));
        AccountEndpoint = endpoint ?? throw new ArgumentNullException(nameof(endpoint));
        AccountKey = null;

        return this;
    }

    /// <summary>
    /// Configure event store to use AuthKey when connecting to cosmos db.
    /// </summary>
    /// <param name="endpoint">Cosmos account endpoint.</param>
    /// <param name="authKey">Authorization key to connect with.</param>
    /// <exception cref="ArgumentNullException">Throws when <paramref name="authKey"/> or <paramref name="endpoint"/> are null.</exception>
    /// <returns>The updated <see cref="DocumentOptions"/>.</returns>
    public DocumentOptions UseCredentials(
        string endpoint,
        string authKey)
    {
        Credential = null;
        AccountEndpoint = endpoint ?? throw new ArgumentNullException(nameof(endpoint));
        AccountKey = authKey ?? throw new ArgumentNullException(nameof(authKey));

        return this;
    }

    /// <summary>
    /// Configure event store to use cosmos emulator.
    /// </summary>
    /// <param name="endpoint">Optional custom cosmos emulator endpoint.</param>
    /// <param name="allowAnyServerCertificate">Optionally configure cosmos client to accept any server certificate.</param>
    /// <returns>The updated <see cref="DocumentOptions"/>.</returns>
    public DocumentOptions UseCosmosEmulator(
        string endpoint = EmulatorEndpoint,
        bool allowAnyServerCertificate = false)
    {
        Credential = null;
        AccountEndpoint = endpoint;
        AccountKey = EmulatorAuthKey;
        AllowAnyServerCertificate = allowAnyServerCertificate;

        return this;
    }

    public DocumentOptions AddContainer<T>(string containerName)
        => AddContainer(typeof(T), containerName);

    public DocumentOptions AddContainer(Type documentType, string containerName)
    {
        containerNames[documentType] = containerName;
        return this;
    }
}
