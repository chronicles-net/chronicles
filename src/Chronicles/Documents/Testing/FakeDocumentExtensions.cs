using System.Text.Json;

namespace Chronicles.Documents.Testing;

public static class FakeDocumentExtensions
{
    public static T DeepClone<T>(
        this T resource,
        JsonSerializerOptions? options = null)
    {
        var json = JsonSerializer.Serialize(resource, options);
        return JsonSerializer.Deserialize<T>(json, options)
            ?? resource;
    }

    public static TResult DeepClone<T, TResult>(
        this T resource,
        JsonSerializerOptions? options = null)
        => JsonSerializer
            .Deserialize<TResult>(
                JsonSerializer
                    .Serialize(resource, options),
                options)
        ?? throw new InvalidOperationException($"Unable to convert {typeof(T).Name} to {typeof(TResult).Name}");

    public static TResult DeepCloneObject<TResult>(
        this object resource,
        JsonSerializerOptions? options = null)
        => JsonSerializer
            .Deserialize<TResult>(
                JsonSerializer
                    .Serialize(resource, resource.GetType(), options),
                options)
        ?? throw new InvalidOperationException($"Unable to convert {resource.GetType().Name} to {typeof(TResult).Name}");

    public static IEnumerable<T> DeepClone<T>(
        this IEnumerable<T> resources,
        JsonSerializerOptions? options = null)
        => resources.Select(r => r.DeepClone(options));

    public static IEnumerable<TResult> DeepClone<T, TResult>(
        this IEnumerable<T> resources,
        JsonSerializerOptions? options = null)
        => resources.Select(r => r.DeepClone<T, TResult>(options));
}
