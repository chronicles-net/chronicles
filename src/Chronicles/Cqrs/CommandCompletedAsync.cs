namespace Chronicles.Cqrs;

/// <summary>
/// Delegate for handling command completion.
/// </summary>
/// <typeparam name="TCommand">Command type</typeparam>
/// <param name="context">Completion context that provides access to</param>
/// <param name="cancellationToken"><seealso cref="CancellationToken"/> representing request cancellation.</param>
/// <returns>A <see cref="ValueTask"/> representing the asynchronous operation.</returns>
public delegate ValueTask CommandCompletedAsync<TCommand>(
    ICommandCompletionContext<TCommand> context,
    CancellationToken cancellationToken)
    where TCommand : class;