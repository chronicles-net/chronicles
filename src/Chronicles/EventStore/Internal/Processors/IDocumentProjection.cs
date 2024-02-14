using Chronicles.Documents;

namespace Chronicles.EventStore.Internal.Processors;

public interface IDocumentProjection<TState>
    where TState : class, IDocument
{
    Task CommitAsync(
        IDocumentWriter<TState> writer,
        CancellationToken cancellationToken);

    Task ResumeAsync(
        IDocumentReader<TState> reader,
        StreamId streamId,
        StreamEvent[] events,
        CancellationToken cancellationToken);
}
