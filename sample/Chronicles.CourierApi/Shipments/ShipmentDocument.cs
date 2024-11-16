using Chronicles.Documents;

namespace Chronicles.CourierApi.Shipments;

[ContainerName("shipments")]
public record ShipmentDocument(
    string Id,
    string Pk)
    : IDocument
{
    /// <summary>
    /// Shipment assigned to courier id. 
    /// </summary>
    public string? CourierId { get; set; }

    public ShipmentState State { get; set; }

    public int ActiveOrders { get; set; }

    string IDocument.GetDocumentId() => Id;

    string IDocument.GetPartitionKey() => Pk;
}
