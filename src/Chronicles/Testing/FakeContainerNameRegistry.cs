using Chronicles.Documents.Internal;

namespace Chronicles.Testing;

public class FakeContainerNameRegistry(
    string containerName)
    : IContainerNameRegistry
{
    public string GetContainerName<T>(
        string? storeName = null)
        => GetContainerName(typeof(T), storeName);

    public string GetContainerName(
        Type documentType,
        string? storeName = null)
        => containerName;
}
