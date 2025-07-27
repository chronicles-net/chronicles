namespace Chronicles.Documents;

/// <summary>
/// Represents a paged result set for queries, including items and a continuation token for pagination.
/// Use this class to return results from queries that support pagination, such as Cosmos DB or other data sources with continuation tokens.
/// </summary>
/// <typeparam name="T">The type of items in the result set.</typeparam>
public class PagedResult<T>
{
    /// <summary>
    /// Gets or sets the items returned in the current page of results.
    /// </summary>
    public IReadOnlyList<T> Items { get; set; } = [];

    /// <summary>
    /// Gets or sets the continuation token used to fetch the next page of results.
    /// If <c>null</c>, there are no more results to fetch.
    /// </summary>
    public string? ContinuationToken { get; set; }
}
