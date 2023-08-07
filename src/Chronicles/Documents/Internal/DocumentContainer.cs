namespace Chronicles.Documents.Internal;

public record DocumentContainer(
    Type DocumentType,
    string ContainerName,
    string? StoreName = null);
