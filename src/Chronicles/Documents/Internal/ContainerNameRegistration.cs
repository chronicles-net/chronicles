namespace Chronicles.Documents.Internal;

public class ContainerNameRegistration
{
    public ContainerNameRegistration(
        Type documentType,
        string containerName,
        string? storeName = null)
    {
        DocumentType = documentType;
        ContainerName = containerName;
        StoreName = storeName;
    }

    public Type DocumentType { get; }

    public string ContainerName { get; }

    public string? StoreName { get; }
}
