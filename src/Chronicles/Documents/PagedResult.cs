namespace Chronicles.Documents;

public class PagedResult<T>
{
    public IReadOnlyList<T> Items { get; set; }
        = Array.Empty<T>();

    public string? ContinuationToken { get; set; }
}
