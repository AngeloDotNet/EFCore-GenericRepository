namespace ClassLibrary.EFCore.Interfaces;

public interface IRepository<TEntity, TKey>
    where TEntity : class, IEntity<TKey>, new()
{
    /// <summary>
    /// Retrieves all entities asynchronously with optional filtering, ordering, and including related entities.
    /// The result is materialized to an <see cref="IReadOnlyList{T}"/> to avoid deferred execution issues
    /// and to make it safe to use outside of the DbContext lifetime.
    /// </summary>
    /// <param name="includes">A function to include related entities.</param>
    /// <param name="filter">A filter expression to apply.</param>
    /// <param name="orderBy">An expression to order the results.</param>
    /// <param name="ascending">A boolean indicating whether the order should be ascending.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>
    /// A task that represents the asynchronous operation. The task result contains a materialized read-only list of <typeparamref name="TEntity"/>.
    /// </returns>
    Task<IReadOnlyList<TEntity>> GetAllAsync(
        Func<IQueryable<TEntity>, IIncludableQueryable<TEntity, object>>? includes = null,
        Expression<Func<TEntity, bool>>? filter = null,
        Expression<Func<TEntity, object>>? orderBy = null,
        bool ascending = true,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves an entity by its identifier asynchronously.
    /// </summary>
    /// <param name="id">The identifier of the entity.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the entity if found; otherwise, <see langword="null" />.</returns>
    Task<TEntity?> GetByIdAsync(TKey id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new entity asynchronously.
    /// </summary>
    /// <param name="entity">The entity to create.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    Task CreateAsync(TEntity entity, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing entity asynchronously.
    /// </summary>
    /// <param name="entity">The entity to update.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    Task UpdateAsync(TEntity entity, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes an entity asynchronously.
    /// </summary>
    /// <param name="entity">The entity to delete.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    Task DeleteAsync(TEntity entity, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes an entity by its identifier asynchronously.
    /// </summary>
    /// <param name="id">The identifier of the entity to delete.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    Task DeleteByIdAsync(TKey id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a paginated result of entities asynchronously with optional filtering, ordering, and including related entities.
    /// </summary>
    /// <param name="pageNumber">The page number to retrieve (1-based).</param>
    /// <param name="pageSize">The size of the page.</param>
    /// <param name="includes">A function to include related entities in the query.</param>
    /// <param name="filter">A filter expression to apply to the entities.</param>
    /// <param name="orderBy">An expression to order the results.</param>
    /// <param name="ascending">A boolean indicating whether the order should be ascending.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>
    /// A task that represents the asynchronous operation. The task result contains a <see cref="PaginatedResult{TEntity}"/>
    /// with the paginated entities.
    /// </returns>
    Task<PaginatedResult<TEntity>> GetAllPagingAsync(
        int pageNumber,
        int pageSize,
        Func<IQueryable<TEntity>, IIncludableQueryable<TEntity, object>>? includes = null,
        Expression<Func<TEntity, bool>>? filter = null,
        Expression<Func<TEntity, object>>? orderBy = null,
        bool ascending = true,
        CancellationToken cancellationToken = default);
}
