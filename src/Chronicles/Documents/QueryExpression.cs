namespace Chronicles.Documents;

public delegate IQueryable<TResult> QueryExpression<in TSource, out TResult>(IQueryable<TSource> source);
