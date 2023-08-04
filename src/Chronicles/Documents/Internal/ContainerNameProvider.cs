using System.Collections.Concurrent;
using System.Reflection;

namespace Chronicles.Documents.Internal;

public class ContainerNameProvider
{
    private static readonly ConcurrentDictionary<Type, ContainerNameAttribute> Attributes = new();

    public ContainerNameAttribute GetContainerName<T>()
        => GetContainerName(typeof(T));

    public ContainerNameAttribute GetContainerName(Type documentType)
    {
        if (Attributes.TryGetValue(documentType, out var result))
        {
            return result;
        }

        if (documentType.GetCustomAttribute<ContainerNameAttribute>(inherit: true) is not { } att)
        {
            throw new ArgumentException(
                $"Type {documentType.Name} is not supported. " +
                $"Missing {nameof(ContainerNameAttribute)}.",
                nameof(documentType));
        }

        return Attributes.GetOrAdd(documentType, att);
    }
}
