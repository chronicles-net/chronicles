using System.Runtime.CompilerServices;
using Microsoft.Azure.Cosmos;

namespace Chronicles.Documents.Internal;

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
}
