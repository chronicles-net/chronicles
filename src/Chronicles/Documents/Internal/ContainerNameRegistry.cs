using System.Collections.Concurrent;
using System.Reflection;

namespace Chronicles.Documents.Internal;

internal class ContainerNameRegistry : IContainerNameRegistry
{
    private readonly ConcurrentDictionary<DocumentTypeKey, string> names;

    public ContainerNameRegistry(
        IEnumerable<IDocumentStore> stores)
    {
        names = new(
            stores
            .SelectMany(s => s
                .Options
                .ContainerNames
                .Select(c => KeyValuePair.Create(
                    new DocumentTypeKey(c.Key, s.Name),
                    c.Value)))
            .ToDictionary(
                r => r.Key,
                r => r.Value));
    }

    public string GetContainerName<T>(
        string? storeName = null)
        => GetContainerName(typeof(T), storeName);

    public string GetContainerName(
        Type documentType,
        string? storeName = null)
    {
        var key = new DocumentTypeKey(documentType, storeName ?? string.Empty);
        if (names.TryGetValue(key, out var containerName))
        {
            return containerName;
        }

        if (documentType.GetCustomAttributes<ContainerNameAttribute>(inherit: true) is { } attributes)
        {
            containerName = attributes
                .Where(a => (a.StoreName ?? string.Empty) == key.StoreName)
                .Select(a => a.ContainerName)
                .FirstOrDefault();

            if (containerName != null)
            {
                return names[key] = containerName;
            }
        }

        var baseType = documentType.BaseType;
        while (baseType != null && baseType != typeof(object))
        {
            key = key with { DocumentType = baseType };
            if (names.TryGetValue(key, out containerName))
            {
                return names[key] = containerName;
            }
            baseType = baseType.BaseType;
        }

        throw new ArgumentException(
            $"Type {documentType.Name} is not supported. " +
            $"Missing {nameof(ContainerNameAttribute)} or " +
            $"container registration in {nameof(DocumentOptions)}.",
            nameof(documentType));
    }
}
