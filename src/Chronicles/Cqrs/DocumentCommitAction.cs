namespace Chronicles.Cqrs;

/// <summary>
/// Specifies the possible actions to take when committing a document.
/// </summary>
public enum DocumentCommitAction
{
    /// <summary>
    /// Update the document.
    /// </summary>
    Update,

    /// <summary>
    /// Delete the document.
    /// </summary>
    Delete,

    /// <summary>
    /// Take no action on the document.
    /// </summary>
    None,
}
