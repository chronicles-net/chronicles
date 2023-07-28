using System.Text.Json;

namespace Chronicles.Cosmos.Testing
{
    public static class CosmosDocumentExtensions
    {
        public static T DeepClone<T>(
            this T resource,
            JsonSerializerOptions? options = null)
        {
            var json = JsonSerializer.Serialize(resource, options);
            return JsonSerializer.Deserialize<T>(json, options)
                ?? resource;
        }

        public static IEnumerable<T> DeepClone<T>(
            this IEnumerable<T> resources,
            JsonSerializerOptions? options = null)
            => resources.Select(r => r.DeepClone(options));
    }
}
