using Chronicles.Documents;

namespace Chronicles.Cqrs;

public interface IDocumentPublisher<TDocument>
    where TDocument : class, IDocument
{
    Task PublishAsync(
        TDocument document,
        CancellationToken cancellationToken);
}
