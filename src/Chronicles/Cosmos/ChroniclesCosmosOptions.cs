using System.Text.Json;
using Azure.Core;

namespace Chronicles.Cosmos;

/// <summary>
/// Options for configuring the connection to Cosmos.
/// </summary>
public class ChroniclesCosmosOptions
{
    /// <summary>
    /// Gets or sets the Cosmos account endpoint URI.
    /// </summary>
    /// <remarks>
    /// You can get this value from the Azure portal.
    /// Navigate to your Azure Cosmos account.
    /// Open the Overview pane and copy the URI value.
    /// </remarks>
    public string AccountEndpoint { get; set; } = default!;

    /// <summary>
    /// Gets or sets the Cosmos account key.
    /// </summary>
    /// <remarks>
    /// You can get this value from the Azure portal.
    /// Navigate to your Azure Cosmos account.
    /// Open the Connection Strings or Keys pane, and copy the
    /// "Password" or PRIMARY KEY value.
    /// The <see cref="AccountKey"/> is required if <see cref="Credential"/> is not specified.
    /// </remarks>
    public string? AccountKey { get; set; } = default!;

    /// <summary>
    /// Gets or sets the TokenCredential to use instead of <see cref="AccountKey"/>.
    /// </summary>
    /// <remarks>
    /// When <see cref="TokenCredential"/> is provided the property <see cref="AccountKey"/> is not needed.
    /// </remarks>
    public TokenCredential? Credential { get; set; }

    /// <summary>
    /// Gets or sets the Cosmos database name.
    /// </summary>
    public string DatabaseName { get; set; } = default!;

    /// <summary>
    /// Gets or sets the options for controlling the json serializer.
    /// </summary>
    public JsonSerializerOptions SerializerOptions { get; set; }
        = new JsonSerializerOptions();
}
