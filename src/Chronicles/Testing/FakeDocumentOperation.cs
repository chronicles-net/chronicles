using Chronicles.Documents;

namespace Chronicles.Testing;

public record FakeDocumentOperation(
    FakeDocumentAction Action,
    IDocument Document);
