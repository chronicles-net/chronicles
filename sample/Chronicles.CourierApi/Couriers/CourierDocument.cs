using Chronicles.Documents;

namespace Chronicles.CourierApi.Couriers;

[ContainerName("courier")]
public record CourierDocument(
    string Id,
    string Pk)
    : IDocument
{
    public string? FirstName { get; set; }

    public string? LastName { get; set; }

    string IDocument.GetDocumentId() => Id;

    string IDocument.GetPartitionKey() => Pk;
}
