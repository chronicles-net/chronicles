namespace Chronicles.Documents.Testing;

public record FakeDocumentOperation(
    FakeDocumentAction Action,
    IDocument Document);
