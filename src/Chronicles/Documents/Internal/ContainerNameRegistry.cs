using System.Collections.Concurrent;
using System.Reflection;

namespace Chronicles.Documents.Internal;

public class ContainerNameRegistry : IContainerNameRegistry
{
    private readonly ConcurrentDictionary<Type, DocumentContainer> names;

    public ContainerNameRegistry(
        IEnumerable<IDocumentStore> stores)
    {
        names = new(
            stores
            .SelectMany(s => s.Options.ContainerNames.Select(c => new DocumentContainer(c.Key, c.Value, s.Name)))
            .GroupBy(r => r.DocumentType)
            .ToDictionary(
                r => r.Key,
                r => r.First()));
    }

    public DocumentContainer GetContainerName<T>()
        => GetContainerName(typeof(T));

    public DocumentContainer GetContainerName(
        Type documentType)
    {
        if (names.TryGetValue(documentType, out var containerName))
        {
            return containerName;
        }

        if (documentType.GetCustomAttribute<ContainerNameAttribute>(inherit: true) is { } attribute)
        {
            return names[documentType] = new(
                documentType,
                attribute.ContainerName,
                attribute.StoreName);
        }

        var baseType = documentType.BaseType;
        while (baseType != null && baseType != typeof(object))
        {
            if (names.TryGetValue(baseType, out containerName))
            {
                return containerName;
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
