using System.Collections.Concurrent;
using System.Reflection;

namespace Chronicles.Documents.Internal;

public class ContainerNameRegistry : IContainerNameRegistry
{
    private readonly ConcurrentDictionary<Type, string> names;

    public ContainerNameRegistry(
        IEnumerable<IDocumentStore> stores)
    {
        names = new(
            stores
            .SelectMany(s => s.Options.ContainerNames)
            .GroupBy(r => r.Key, r => r.Value)
            .ToDictionary(
                r => r.Key,
                r => r.First()));
    }

    public string GetContainerName<T>()
        => GetContainerName(typeof(T));

    public string GetContainerName(
        Type documentType)
    {
        if (names.TryGetValue(documentType, out var containerName))
        {
            return containerName;
        }

        if (documentType.GetCustomAttribute<ContainerNameAttribute>(inherit: true) is { } attribute)
        {
            return names[documentType] = attribute.ContainerName;
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
