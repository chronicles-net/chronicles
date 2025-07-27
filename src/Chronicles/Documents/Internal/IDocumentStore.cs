namespace Chronicles.Documents.Internal;

internal interface IDocumentStore
{
    string Name { get; }

    DocumentOptions Options { get; }
}
