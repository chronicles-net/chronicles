using Chronicles.Documents;

namespace Chronicles.Cqrs;

public interface IDocumentProjection<TDocument>
    : IStateProjection<TDocument>
    where TDocument : class, IDocument
{
}