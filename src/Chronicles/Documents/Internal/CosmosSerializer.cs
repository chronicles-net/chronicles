using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Text.Json;
using Microsoft.Azure.Cosmos;

namespace Chronicles.Documents.Internal;

/// <summary>
/// Implementation used for serializing a stream to and from Json using the <seealso cref="JsonSerializer"/>
/// from within Cosmos SDK.
/// </summary>
public class CosmosSerializer : ICosmosSerializer
{
    private readonly JsonSerializerOptions options;

    public CosmosSerializer(
        JsonSerializerOptions options)
    {
        this.options = options;
        PropertyNamingPolicy = options.PropertyNamingPolicy == JsonNamingPolicy.CamelCase
            ? CosmosPropertyNamingPolicy.CamelCase
            : CosmosPropertyNamingPolicy.Default;
    }

    public CosmosPropertyNamingPolicy PropertyNamingPolicy { get; }

    [return: MaybeNull]
    public T FromStream<T>(Stream stream)
    {
        ArgumentNullException.ThrowIfNull(stream);

        using (stream)
        {
            if (stream.CanSeek && stream.Length == 0)
            {
                return default;
            }

            // This part is taken from one of the Cosmos samples.
            if (typeof(Stream).IsAssignableFrom(typeof(T)))
            {
                return (T)(object)stream;
            }

            // Response data from cosmos always comes as a memory stream.
            // Note: This might change in v4, but so far it doesn't look like it.
            if (stream is MemoryStream memoryStream && memoryStream.TryGetBuffer(out var buffer))
            {
                return JsonSerializer.Deserialize<T>(buffer, options);
            }

            return default;
        }
    }

    public Stream ToStream<T>(T input)
    {
        if (input is null)
        {
            throw new ArgumentNullException(nameof(input));
        }

        var streamPayload = new MemoryStream();

        using var utf8JsonWriter = new Utf8JsonWriter(
            streamPayload,
            new JsonWriterOptions
            {
                Indented = options.WriteIndented,
            });

        JsonSerializer.Serialize(utf8JsonWriter, input, options);
        streamPayload.Position = 0;

        return streamPayload;
    }

    [return: MaybeNull]
    public T FromString<T>(string json)
        => JsonSerializer.Deserialize<T>(
            json,
            options);

    public string SerializeMemberName(MemberInfo memberInfo)
        => options.PropertyNamingPolicy?.ConvertName(memberInfo.Name) ??
           memberInfo.Name;
}