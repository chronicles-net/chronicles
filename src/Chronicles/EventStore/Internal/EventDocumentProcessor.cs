using Chronicles.Documents;

namespace Chronicles.EventStore.Internal;

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
            .Select(p => p.ProcessAsync(changes, cancellationToken))
            .ToArray();

        try
        {
            await Task.WhenAll(processorTasks);
        }
        catch (Exception e)
        {
            await exceptionHandler.HandleAsync(e);
        }
    }
}