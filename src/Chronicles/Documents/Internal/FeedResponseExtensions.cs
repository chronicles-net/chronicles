using Microsoft.Azure.Cosmos;

namespace Chronicles.Documents.Internal;

internal static class FeedResponseExtensions
{
    public static async Task<IEnumerable<T>> GetItemsAsync<T>(
        this Task<FeedResponse<T>> responseTask)
    {
        var response = await responseTask.ConfigureAwait(false);
        return response.Resource;
    }
}
