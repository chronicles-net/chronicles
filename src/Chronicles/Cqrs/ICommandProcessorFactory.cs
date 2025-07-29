namespace Chronicles.Cqrs;

/// <summary>
/// Factory interface for creating command processor instances for specific command types.
/// </summary>
/// <remarks>
/// Implementations of this interface are responsible for providing <see cref="ICommandProcessor{TCommand}"/> instances
/// that can execute commands of the specified type. This is typically used to abstract the creation logic and allow for
/// dependency injection or custom processor resolution strategies.
/// </remarks>
public interface ICommandProcessorFactory
{
    /// <summary>
    /// Creates a command processor for the specified command type.
    /// </summary>
    /// <typeparam name="TCommand">The type of command to be processed. Must be a reference type.</typeparam>
    /// <returns>
    /// An <see cref="ICommandProcessor{TCommand}"/> instance capable of executing commands of type <typeparamref name="TCommand"/>.
    /// </returns>
    ICommandProcessor<TCommand> Create<TCommand>()
        where TCommand : class;
}
