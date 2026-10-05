namespace Infrastructure.Extensions;

/// <summary>
/// Extension methods for enhancing Entity Framework Core functionality,
/// including soft delete filtering, pagination, and dynamic query filtering.
/// </summary>
public static class EfCore
{
    /// <summary>
    /// Applies a global query filter to exclude entities with <c>IsDeleted</c> set to <c>true</c>.
    /// Targets all entities inheriting from <see cref="BaseEntity"/>.
    /// </summary>
    /// <param name="modelBuilder">EF Core model builder instance.</param>
    public static void FilterSoftDeletedProperties(this ModelBuilder modelBuilder)
    {
        Expression<Func<BaseEntity, bool>> filterExpr = e => !e.IsDeleted;
        foreach (IMutableEntityType mutableEntityType in modelBuilder.Model.GetEntityTypes()
                     .Where(m => m.ClrType.IsAssignableTo(typeof(BaseEntity))))
        {
            ParameterExpression parameter = Expression.Parameter(mutableEntityType.ClrType);
            Expression body = ReplacingExpressionVisitor
                .Replace(filterExpr.Parameters.First(), parameter, filterExpr.Body);
            LambdaExpression lambdaExpression = Expression.Lambda(body, parameter);

            mutableEntityType.SetQueryFilter(lambdaExpression);
        }
    }

    /// <summary>
    /// Applies pagination to an <see cref="IEnumerable{T}"/> collection.
    /// </summary>
    /// <typeparam name="T">Entity type.</typeparam>
    /// <param name="entity">The source collection.</param>
    /// <param name="pageNumber">The 1-based page number.</param>
    /// <param name="pageSize">Number of items per page.</param>
    /// <returns>Paginated result set.</returns>
    public static IEnumerable<T> Page<T>(this IEnumerable<T> entity, int pageNumber, int pageSize)
        => entity.Skip((pageNumber - 1) * pageSize).Take(pageSize).ToList();

    /// <summary>
    /// Applies pagination to an <see cref="IQueryable{T}"/> query.
    /// </summary>
    /// <typeparam name="T">Entity type.</typeparam>
    /// <param name="entity">The source queryable.</param>
    /// <param name="pageNumber">The 1-based page number.</param>
    /// <param name="pageSize">Number of items per page.</param>
    /// <returns>Paginated queryable result set.</returns>
    public static IQueryable<T> Page<T>(this IQueryable<T> entity, int pageNumber, int pageSize)
        => entity.Skip((pageNumber - 1) * pageSize).Take(pageSize);


    /// <summary>
    /// Applies a dynamic <c>LIKE</c>-based filter on a string property of the given query.
    /// Falls back to case-insensitive <c>Contains</c> filtering if <c>LIKE</c> is unavailable.
    /// </summary>
    /// <typeparam name="T">Entity type.</typeparam>
    /// <param name="query">The queryable collection to filter.</param>
    /// <param name="filterValue">The string value to search for.</param>
    /// <param name="propertySelector">Expression to select the string property to filter by.</param>
    /// <returns>A filtered <see cref="IQueryable{T}"/> based on the provided value.</returns>
    public static IQueryable<T> ApplyFilter<T>(
        this IQueryable<T> query,
        string? filterValue,
        Expression<Func<T, string?>> propertySelector)
    {
        if (string.IsNullOrWhiteSpace(filterValue)) return query;

        ParameterExpression parameter = propertySelector.Parameters[0];
        Expression property = propertySelector.Body;

        MethodInfo? likeMethod = typeof(DbFunctionsExtensions).GetMethod(
            nameof(DbFunctionsExtensions.Like),
            [
                typeof(DbFunctions),
                typeof(string),
                typeof(string)
            ]
        );

        Expression<Func<T, bool>> predicate;

        if (likeMethod != null)
        {
            MethodCallExpression likeCall = Expression.Call(
                likeMethod,
                Expression.Constant(EF.Functions),
                property,
                Expression.Constant($"%{filterValue}%")
            );

            predicate = Expression.Lambda<Func<T, bool>>(likeCall, parameter);
        }
        else
        {
            MethodInfo? toLowerMethod = typeof(string).GetMethod(nameof(string.ToLower), Type.EmptyTypes);
            MethodInfo? containsMethod = typeof(string).GetMethod(nameof(string.Contains), new[] { typeof(string) });

            MethodCallExpression lowerProperty = Expression.Call(property, toLowerMethod!);
            ConstantExpression lowerFilter = Expression.Constant(filterValue.ToLower());

            MethodCallExpression containsCall = Expression.Call(lowerProperty, containsMethod!, lowerFilter);

            predicate = Expression.Lambda<Func<T, bool>>(containsCall, parameter);
        }

        return query.Where(predicate);
    }

    public static IQueryable<T> WhereIf<T>(this IQueryable<T> source, bool condition, Expression<Func<T, bool>> predicate)
        => condition ? source.Where(predicate) : source;
}