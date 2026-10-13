namespace ClassLibrary.EFCore;

/// <summary>
/// Provides CRUD and query operations for entities of type <typeparamref name="TEntity"/> using an Entity Framework
/// Core <see cref="DbContext"/>. Query methods support optional includes, filtering, ordering, and pagination;
/// asynchronous methods accept a <see cref="CancellationToken"/>.
/// </summary>
/// <remarks>Methods use no-tracking queries and detach entities to avoid persistent tracking and deferred
/// execution outside the context lifetime. Create, update, and delete operations wrap EF Core update exceptions in
/// InvalidOperationException with contextual messages. Paging methods perform a separate count query to avoid including
/// navigation properties in the count.</remarks>
/// <typeparam name="TEntity">The entity type managed by the repository; must implement <see cref="IEntity{TKey}"/>, be a reference type, and have
/// a parameterless constructor.</typeparam>
/// <typeparam name="TKey">The type of the entity identifier.</typeparam>
/// <param name="dbContext">The Entity Framework Core <see cref="DbContext"/> used to access the database; must not be <see langword="null"/>.</param>
public class Repository<TEntity, TKey>(DbContext dbContext) : IRepository<TEntity, TKey> where TEntity : class, IEntity<TKey>, new()
{
    /// <summary>
    /// Gets the <see cref="DbContext" /> instance provided to the containing type.
    /// </summary>
    /// <remarks>Initialized by the constructor; the property is guaranteed non-<see langword="null" />.
    /// Passing a <see langword="null" /> dbContext to the constructor throws an <see cref="ArgumentNullException" />.</remarks>
    public DbContext DbContext { get; } = dbContext ?? throw new ArgumentNullException(nameof(dbContext));

    /// <summary>
    /// Retrieves entities asynchronously from the DbContext applying optional eager-loading, filtering, and ordering;
    /// entities are materialized without change tracking.
    /// </summary>
    /// <remarks>The query is executed with AsNoTracking to avoid change tracking and uses
    /// ConfigureAwait(false). Provide includes and filters to limit transferred data and improve performance.</remarks>
    /// <param name="includes">A function that applies EF Core Include/ThenInclude operations to the query; pass <see langword="null"/> to
    /// include no related entities.</param>
    /// <param name="filter">A predicate expression to filter the returned entities; pass <see langword="null"/> to return all entities.</param>
    /// <param name="orderBy">A key selector expression to order the results; pass <see langword="null"/> to preserve the store's default
    /// ordering.</param>
    /// <param name="ascending">When <see langword="true"/>, order ascending; when <see langword="false"/>, order descending.</param>
    /// <param name="cancellationToken">A <see cref="System.Threading.CancellationToken"/> to cancel the asynchronous operation.</param>
    /// <returns>A task that resolves to a read-only list of entities matching the specified criteria; entities are returned not
    /// tracked by the DbContext.</returns>
    public async Task<IReadOnlyList<TEntity>> GetAllAsync(Func<IQueryable<TEntity>, IIncludableQueryable<TEntity, object>>? includes = null,
        Expression<Func<TEntity, bool>>? filter = null, Expression<Func<TEntity, object>>? orderBy = null, bool ascending = true, CancellationToken cancellationToken = default)
    {
        var set = DbContext.Set<TEntity>().AsNoTracking();
        IQueryable<TEntity> query = set;

        if (includes is not null)
        {
            query = includes(query);
        }

        if (filter is not null)
        {
            query = query.Where(filter);
        }

        if (orderBy is not null)
        {
            query = ascending ? query.OrderBy(orderBy) : query.OrderByDescending(orderBy);
        }

        var list = await query.ToListAsync(cancellationToken).ConfigureAwait(false);

        return list;
    }

    /// <summary>
    /// Gets an entity by its primary key asynchronously and detaches it from the repository DbContext so it is not
    /// tracked.
    /// </summary>
    /// <remarks>The retrieved entity is detached from the DbContext to avoid tracking by the
    /// repository.</remarks>
    /// <param name="id">The primary key value of the entity to retrieve.</param>
    /// <param name="cancellationToken">A cancellation token to cancel the asynchronous operation.</param>
    /// <returns>The entity with the specified key if found; otherwise, <see langword="null" />.</returns>
    public async Task<TEntity?> GetByIdAsync(TKey id, CancellationToken cancellationToken = default)
    {
        // FindAsync requires object[] for composite keys; single key wrapped in object[]
        var entity = await DbContext.Set<TEntity>().FindAsync([id], cancellationToken).ConfigureAwait(false);

        if (entity is null)
        {
            return null;
        }

        // Detach to avoid keeping the entity tracked by the repository's context
        DbContext.Entry(entity).State = EntityState.Detached;

        return entity;
    }

    /// <summary>
    /// Creates the specified entity in the database asynchronously, saves changes, and detaches the entity from the
    /// DbContext.
    /// </summary>
    /// <remarks>Throws ArgumentNullException if <see langword="null" />. After a successful save the entity
    /// is detached from the DbContext to avoid long‑lived tracking.</remarks>
    /// <param name="entity">The entity to add and persist.</param>
    /// <param name="cancellationToken">A cancellation token to observe while performing the operation.</param>
    /// <returns>A task that represents the asynchronous create operation.</returns>
    /// <exception cref="InvalidOperationException">Thrown if saving changes fails; a DbUpdateException is wrapped as the inner exception.</exception>
    public async Task CreateAsync(TEntity entity, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entity);

        var set = DbContext.Set<TEntity>();

        await set.AddAsync(entity, cancellationToken).ConfigureAwait(false);

        try
        {
            await DbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (DbUpdateException ex)
        {
            throw new InvalidOperationException($"Failed to create entity of type {typeof(TEntity).Name}.", ex);
        }

        // Detach to avoid holding references in the context
        DbContext.Entry(entity).State = EntityState.Detached;
    }

    /// <summary>
    /// Updates the specified <typeparamref name="TEntity" /> in the DbContext and persists changes asynchronously.
    /// </summary>
    /// <remarks>After a successful save the entity is detached from the DbContext. An <see
    /// cref="ArgumentNullException" /> is thrown if <paramref name="entity" /> is <see langword="null" />.</remarks>
    /// <param name="entity">The entity to update; must not be <see langword="null" />.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the asynchronous operation.</param>
    /// <returns>A <see cref="Task" /> that represents the asynchronous operation.</returns>
    /// <exception cref="InvalidOperationException">Thrown when a concurrency conflict occurs while saving changes; the original <see
    /// cref="Microsoft.EntityFrameworkCore.DbUpdateConcurrencyException" /> is provided as the inner exception.</exception>
    public async Task UpdateAsync(TEntity entity, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entity);

        var set = DbContext.Set<TEntity>();
        set.Update(entity);

        try
        {
            await DbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            throw new InvalidOperationException($"Concurrency conflict when updating entity of type {typeof(TEntity).Name}.", ex);
        }

        DbContext.Entry(entity).State = EntityState.Detached;
    }

    /// <summary>
    /// Deletes the specified entity from the DbContext and persists the change asynchronously.
    /// </summary>
    /// <remarks>Validates that the entity is not null and throws <see cref="ArgumentNullException"/> when it
    /// is. Calls <see
    /// cref="Microsoft.EntityFrameworkCore.DbContext.SaveChangesAsync(System.Threading.CancellationToken)"/> and wraps
    /// any <see cref="Microsoft.EntityFrameworkCore.DbUpdateException"/> in an <see
    /// cref="InvalidOperationException"/>.</remarks>
    /// <param name="entity">The entity to delete.</param>
    /// <param name="cancellationToken">A cancellation token to cancel the asynchronous operation.</param>
    /// <returns>A task that represents the asynchronous delete operation; completes when the entity has been removed and changes
    /// have been saved.</returns>
    /// <exception cref="InvalidOperationException">Thrown if saving changes to the database fails; the original DbUpdateException is provided as the inner
    /// exception.</exception>
    public async Task DeleteAsync(TEntity entity, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entity);

        var set = DbContext.Set<TEntity>();
        set.Remove(entity);

        try
        {
            await DbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (DbUpdateException ex)
        {
            throw new InvalidOperationException($"Failed to delete entity of type {typeof(TEntity).Name}.", ex);
        }
    }

    /// <summary>
    /// Deletes the entity with the specified primary key from the database asynchronously.
    /// </summary>
    /// <remarks>Attaches a stub entity with the specified key to the DbContext and removes it to avoid an
    /// extra database round-trip.</remarks>
    /// <param name="id">The primary key value of the entity to delete.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation of the asynchronous operation.</param>
    /// <returns>A task that represents the asynchronous delete operation.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the underlying database update fails while attempting to delete the entity; the original
    /// DbUpdateException is wrapped as the inner exception.</exception>
    public async Task DeleteByIdAsync(TKey id, CancellationToken cancellationToken = default)
    {
        // Create a stub entity with key set to avoid an extra database round-trip
        var entity = new TEntity { Id = id };

        DbContext.Set<TEntity>().Attach(entity);
        DbContext.Set<TEntity>().Remove(entity);

        try
        {
            await DbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (DbUpdateException ex)
        {
            throw new InvalidOperationException($"Failed to delete entity of type {typeof(TEntity).Name} with id '{id}'.", ex);
        }
    }

    /// <summary>
    /// Retrieves a single page of entities matching the optional filter, applying optional eager-loading includes and
    /// ordering, and returns pagination metadata.
    /// </summary>
    /// <remarks>Counting is performed against a query without includes or ordering to improve performance.
    /// The query is executed with AsNoTracking and uses Skip/Take for paging. The method honors the provided
    /// CancellationToken and uses ConfigureAwait(false) when awaiting database operations.</remarks>
    /// <param name="pageNumber">One-based index of the page to return. Must be greater than or equal to <see langword="1" />.</param>
    /// <param name="pageSize">Number of items per page. Must be greater than <see langword="0" />.</param>
    /// <param name="includes">Function that applies eager-loading includes to the query, or <see langword="null" /> to omit related-entity
    /// includes.</param>
    /// <param name="filter">Predicate expression used to filter the result set, or <see langword="null" /> to include all entities.</param>
    /// <param name="orderBy">Key selector expression used to order the results, or <see langword="null" /> to leave ordering unspecified.</param>
    /// <param name="ascending">If <see langword="true" />, order ascending; if <see langword="false" />, order descending.</param>
    /// <param name="cancellationToken">Token to observe while waiting for the task to complete; supports canceling the database query and defaults to
    /// <see langword="default" />.</param>
    /// <returns>A <see cref="PaginatedResult{TEntity}" /> containing the requested page of items and pagination metadata
    /// (CurrentPage, PageSize, TotalItems, Items).</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="pageNumber" /> is less than 1 or <paramref name="pageSize" /> is less than or equal
    /// to 0.</exception>
    public async Task<PaginatedResult<TEntity>> GetAllPagingAsync(int pageNumber, int pageSize, Func<IQueryable<TEntity>, IIncludableQueryable<TEntity, object>>? includes = null,
        Expression<Func<TEntity, bool>>? filter = null, Expression<Func<TEntity, object>>? orderBy = null, bool ascending = true, CancellationToken cancellationToken = default)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(pageNumber, 1, nameof(pageNumber));
        ArgumentOutOfRangeException.ThrowIfLessThan(pageSize, 1, nameof(pageSize));

        var set = DbContext.Set<TEntity>().AsNoTracking();

        // Base query used for counting - avoid includes/ordering for the count to improve performance
        IQueryable<TEntity> countQuery = set;

        if (filter is not null)
        {
            countQuery = countQuery.Where(filter);
        }

        var totalItems = await countQuery.CountAsync(cancellationToken).ConfigureAwait(false);

        // Query used for fetching page - includes and ordering matter here
        IQueryable<TEntity> query = set;

        if (includes is not null)
        {
            query = includes(query);
        }

        if (filter is not null)
        {
            query = query.Where(filter);
        }

        if (orderBy is not null)
        {
            query = ascending ? query.OrderBy(orderBy) : query.OrderByDescending(orderBy);
        }

        var skip = (pageNumber - 1) * pageSize;

        var items = await query
            .Skip(skip)
            .Take(pageSize)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return new PaginatedResult<TEntity>
        {
            CurrentPage = pageNumber,
            PageSize = pageSize,
            TotalItems = totalItems,
            Items = items
        };
    }
}