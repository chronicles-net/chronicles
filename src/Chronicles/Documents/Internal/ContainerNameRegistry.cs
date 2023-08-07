using System.Collections.Concurrent;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;

namespace Chronicles.Documents.Internal;

public class ContainerNameRegistry : IContainerNameRegistry
{
    private readonly ConcurrentDictionary<Type, ContainerNameRegistration> names;

    public ContainerNameRegistry(
        IEnumerable<ContainerNameRegistration> registrations)
    {
        this.names = new(registrations
            .GroupBy(r => r.DocumentType)
            .ToDictionary(
                r => r.Key,
                r => r.First()));
    }

    public ContainerNameRegistration GetContainerName<T>()
        => GetContainerName(typeof(T));

    public ContainerNameRegistration GetContainerName(
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
            $"registration via the {nameof(ChroniclesBuilder)}.{nameof(ChroniclesBuilder.AddContainer)}.",
            nameof(documentType));
    }
}
