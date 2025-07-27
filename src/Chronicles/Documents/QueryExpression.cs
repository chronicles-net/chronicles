namespace Chronicles.Documents;

/// <summary>
/// Represents a strongly-typed query expression for transforming an <see cref="IQueryable{TSource}"/> into an <see cref="IQueryable{TResult}"/>.
/// Use this delegate to define reusable LINQ queries for filtering, projecting, or shaping data in document queries, such as with Cosmos DB.
/// </summary>
/// <typeparam name="TSource">The type of the source elements.</typeparam>
/// <typeparam name="TResult">The type of the result elements.</typeparam>
/// <param name="source">The source queryable collection to apply the expression to.</param>
/// <returns>An <see cref="IQueryable{TResult}"/> representing the transformed query.</returns>
public delegate IQueryable<TResult> QueryExpression<in TSource, out TResult>(IQueryable<TSource> source);
