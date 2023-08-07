using System.Collections.Concurrent;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;

namespace Chronicles.Documents.Internal;

public class ContainerNameRegistry : IContainerNameRegistry
{
    private readonly ConcurrentDictionary<Type, ContainerNameAttribute> names = new();

    public void AddContainerName<T>(
        string containerName,
        string? storeName = null)
        => AddContainerName(typeof(T), containerName, storeName);

    public void AddContainerName(
        Type documentType,
        string containerName,
        string? storeName = null)
        => names[documentType] = new(containerName)
        {
            StoreName = storeName,
        };

    public ContainerNameAttribute GetContainerName<T>()
        => GetContainerName(typeof(T));

    public ContainerNameAttribute GetContainerName(
        Type documentType)
    {
        if (names.TryGetValue(documentType, out var containerName))
        {
            return containerName;
        }

        if (documentType.GetCustomAttribute<ContainerNameAttribute>(inherit: true) is { } result)
        {
            return names[documentType] = result;
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
            $"registration via the {nameof(ChroniclesBuilder)}.{nameof(ChroniclesBuilder.AddContainer)}.",
            nameof(documentType));
    }
}
