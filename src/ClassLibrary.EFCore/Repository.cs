namespace ClassLibrary.EFCore;

public class Repository<TEntity, TKey>(DbContext dbContext) : IRepository<TEntity, TKey>
    where TEntity : class, IEntity<TKey>, new()
{
    /// <summary>
    /// Gets the database context.
    /// </summary>
    public DbContext DbContext { get; } = dbContext ?? throw new ArgumentNullException(nameof(dbContext));

    /// <summary>
    /// Retrieves all entities asynchronously with optional filtering, ordering, and including related entities.
    /// The result is materialized to a read-only list to avoid deferred execution outside the DbContext lifetime.
    /// </summary>
    public async Task<IReadOnlyList<TEntity>> GetAllAsync(Func<IQueryable<TEntity>, IIncludableQueryable<TEntity, object>>? includes = null,
        Expression<Func<TEntity, bool>>? filter = null, Expression<Func<TEntity, object>>? orderBy = null, bool ascending = true,
        CancellationToken cancellationToken = default)
    {
        var query = DbContext.Set<TEntity>().AsNoTracking();

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
    /// Retrieves an entity by its identifier asynchronously.
    /// </summary>
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
    /// Creates a new entity asynchronously.
    /// </summary>
    public async Task CreateAsync(TEntity entity, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entity);

        DbContext.Set<TEntity>().Add(entity);

        await DbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        // Detach to avoid holding references in the context
        DbContext.Entry(entity).State = EntityState.Detached;
    }

    /// <summary>
    /// Updates an existing entity asynchronously.
    /// </summary>
    public async Task UpdateAsync(TEntity entity, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entity);

        DbContext.Set<TEntity>().Update(entity);

        await DbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        DbContext.Entry(entity).State = EntityState.Detached;
    }

    /// <summary>
    /// Deletes an entity asynchronously.
    /// </summary>
    public async Task DeleteAsync(TEntity entity, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entity);

        DbContext.Set<TEntity>().Remove(entity);

        await DbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Deletes an entity by its identifier asynchronously.
    /// </summary>
    public async Task DeleteByIdAsync(TKey id, CancellationToken cancellationToken = default)
    {
        // Create a stub entity with key set to avoid an extra database round-trip
        var entity = new TEntity { Id = id };

        DbContext.Set<TEntity>().Attach(entity);
        DbContext.Set<TEntity>().Remove(entity);

        await DbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Retrieves a paginated result of entities asynchronously with optional filtering, ordering, and including related entities.
    /// </summary>
    public async Task<PaginatedResult<TEntity>> GetAllPagingAsync(int pageNumber, int pageSize, Func<IQueryable<TEntity>, IIncludableQueryable<TEntity, object>>? includes = null,
        Expression<Func<TEntity, bool>>? filter = null, Expression<Func<TEntity, object>>? orderBy = null, bool ascending = true, CancellationToken cancellationToken = default)
    {
        if (pageNumber < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(pageNumber), "Page number must be greater than or equal to 1.");
        }

        if (pageSize <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(pageSize), "Page size must be greater than 0.");
        }

        // Base query used for counting - avoid includes/ordering for the count to improve performance
        var countQuery = DbContext.Set<TEntity>().AsNoTracking();

        if (filter is not null)
        {
            countQuery = countQuery.Where(filter);
        }

        var totalItems = await countQuery.CountAsync(cancellationToken).ConfigureAwait(false);

        // Query used for fetching page - includes and ordering matter here
        var query = DbContext.Set<TEntity>().AsNoTracking();

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

        var items = await query
            .Skip((pageNumber - 1) * pageSize)
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