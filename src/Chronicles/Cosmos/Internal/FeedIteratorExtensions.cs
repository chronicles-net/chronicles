using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Text.Json;
using Chronicles.Cosmos.Serialization;
using Microsoft.Azure.Cosmos;

namespace Chronicles.Cosmos.Internal;

public static class FeedIteratorExtensions
{
    public static async IAsyncEnumerable<T> ToAsyncEnumerable<T>(
        this FeedIterator<T> reader,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        while (reader.HasMoreResults && !cancellationToken.IsCancellationRequested)
        {
            var documents = await reader
                .ReadNextAsync(cancellationToken)
                .ConfigureAwait(false);

            foreach (var document in documents)
            {
                yield return document;
            }
        }
    }

    public static async IAsyncEnumerable<T> ToAsyncEnumerable<T>(
        this FeedIterator<object> reader,
        IJsonCosmosSerializer serializer,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        while (reader.HasMoreResults && !cancellationToken.IsCancellationRequested)
        {
            var result = await reader
                .ReadNextAsync(cancellationToken)
                .ConfigureAwait(false);

            var documents = result.Parse<T>(serializer);
            foreach (var document in documents)
            {
                yield return document;
            }
        }
    }

    public static async Task<PagedResult<T>> ReadPageResultAsync<T>(
        this FeedIterator<T> reader,
        CancellationToken cancellationToken)
    {
        if (!reader.HasMoreResults)
        {
            return new PagedResult<T>();
        }

        var result = await reader
            .ReadNextAsync(cancellationToken)
            .ConfigureAwait(false);

        return new PagedResult<T>
        {
            Items = result.ToArray(),
            ContinuationToken = result.ContinuationToken,
        };
    }

    public static async Task<PagedResult<T>> ReadPageResultAsync<T>(
        this FeedIterator<object> reader,
        IJsonCosmosSerializer serializer,
        CancellationToken cancellationToken)
    {
        if (!reader.HasMoreResults)
        {
            return new PagedResult<T>();
        }

        var result = await reader
            .ReadNextAsync(cancellationToken)
            .ConfigureAwait(false);

        return new PagedResult<T>
        {
            Items = result
                .Parse<T>(serializer)
                .ToArray(),
            ContinuationToken = result.ContinuationToken,
        };
    }

    private static IEnumerable<T> Parse<T>(
        this IEnumerable<object> documents,
        IJsonCosmosSerializer serializer)
    {
        foreach (var document in documents)
        {
            if (TryParse<T>(document, serializer, out var item))
            {
                yield return item;
            }
        }
    }

    private static bool TryParse<T>(
        object document,
        IJsonCosmosSerializer serializer,
        [NotNullWhen(true)]
        out T? result)
    {
        result = default;
        if (document is not JsonElement json)
        {
            return false;
        }

        var etag = json.TryGetProperty(CosmosFieldNames.ETag, out var value)
            ? value.GetString()
            : null;
        if (serializer.FromJson<T>(json) is { } obj)
        {
            if (obj is ICosmosDocument { ETag: null } doc && etag != null)
            {
                doc.ETag = etag;
            }

            result = obj;
            return true;
        }

        return false;
    }
}
