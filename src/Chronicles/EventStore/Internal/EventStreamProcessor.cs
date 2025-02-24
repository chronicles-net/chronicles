namespace Chronicles.EventStore.Internal;

internal class EventStreamProcessor(
    string? categoryName,
    IEnumerable<IEventProcessor> processors)
    : IEventStreamProcessor
{
    private readonly string categoryName = categoryName ?? string.Empty;
    private readonly IEnumerable<IEventProcessor> processors = processors;

    public async Task ProcessAsync(
      IReadOnlyCollection<StreamEvent> changes,
      CancellationToken cancellationToken)
    {
        var groups = changes
            .Where(e => e.Data is not StreamMetadataDocument)
            .Where(e => string.IsNullOrEmpty(categoryName)
                     || e.Metadata.StreamId.Category.Equals(categoryName, StringComparison.OrdinalIgnoreCase))
            .GroupBy(e => e.Metadata.StreamId);

        foreach (var processor in processors)
        {
            foreach (var group in groups)
            {
                var count = group.Count();
                var context = new StateContext();
                foreach (var evt in group)
                {
                    await processor.ConsumeAsync(
                        evt,
                        context,
                        hasMore: --count > 0,
                        cancellationToken);
                }
            }
        }
    }
}
