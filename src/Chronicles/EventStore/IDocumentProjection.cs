using Chronicles.Documents;

namespace Chronicles.EventStore;

public interface IDocumentProjection<TState>
    where TState : class, IDocument
{
    Task CommitAsync(
        ProjectionKind kind,
        IDocumentWriter<TState> writer,
        CancellationToken cancellationToken);

    Task ResumeAsync(
        ProjectionKind kind,
        IDocumentReader<TState> reader,
        StreamId streamId,
        StreamEvent[] events,
        CancellationToken cancellationToken);
}