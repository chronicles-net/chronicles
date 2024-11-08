using Chronicles.Documents;

namespace Chronicles.EventStore.Internal;

/// <summary>
/// Document processor responsible for starting <see cref="IEventStreamProcessor"/> instances in parallel."/>
/// </summary>
/// <param name="exceptionHandler">Exception handler</param>
/// <param name="processors">Event stream processors</param>
internal class EventDocumentProcessor(
    IEventSubscriptionExceptionHandler exceptionHandler,
    IEnumerable<IEventStreamProcessor> processors)
    : IDocumentProcessor<StreamEvent>
{
    public async Task ErrorAsync(
      string leaseToken,
      Exception exception)
      => await exceptionHandler.HandleAsync(exception);

    public async Task ProcessAsync(
      IReadOnlyCollection<StreamEvent> changes,
      CancellationToken cancellationToken)
    {
        var processorTasks = processors
            .Select(async p => await ExecuteProcessorAsync(changes, p, cancellationToken))
            .ToArray();

        await Task.WhenAll(processorTasks);
    }

    private async Task ExecuteProcessorAsync(
        IReadOnlyCollection<StreamEvent> changes,
        IEventStreamProcessor processor,
        CancellationToken cancellationToken)
    {
        try
        {
            await processor.ProcessAsync(changes, cancellationToken);
        }
        catch (Exception e)
        {
            await exceptionHandler.HandleAsync(e);
        }
    }
}