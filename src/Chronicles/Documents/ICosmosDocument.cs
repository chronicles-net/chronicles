namespace Chronicles.Documents;

/// <summary>
/// Represents a resource that can exist as a document in a Cosmos collection.
/// </summary>
public interface ICosmosDocument
{
    /// <summary>
    /// Gets the id of the Cosmos document.
    /// </summary>
    /// <returns>The id of the Cosmos document.</returns>
    string GetDocumentId();

    /// <summary>
    /// Gets the partition key of the Cosmos document.
    /// </summary>
    /// <returns>The partition key of the Cosmos document.</returns>
    string GetPartitionKey();
}
