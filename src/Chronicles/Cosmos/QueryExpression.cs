namespace Chronicles.Cosmos;

public delegate IQueryable<TResult> QueryExpression<in TSource, out TResult>(IQueryable<TSource> source);
