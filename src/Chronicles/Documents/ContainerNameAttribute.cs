namespace Chronicles.Documents;

/// <summary>
/// Attribute specifying which container to use for the annotated type.
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = true, AllowMultiple = false)]
public sealed class ContainerNameAttribute : Attribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ContainerNameAttribute"/> class.
    /// </summary>
    /// <param name="containerName">The name of the container to use for the annotated type.</param>
    public ContainerNameAttribute(
        string containerName)
    {
        ContainerName = containerName;
    }

    /// <summary>
    /// Gets the name of the container to use for the annoteted type.
    /// </summary>
    public string ContainerName { get; }

    /// <summary>
    /// Gets of sets the name of the configured document store for the container.
    /// </summary>
    public string? StoreName { get; set; }
}
