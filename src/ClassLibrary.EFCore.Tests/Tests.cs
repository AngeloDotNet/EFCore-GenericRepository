using Bogus;
using ClassLibrary.EFCore.Tests.DatabaseContext;
using Microsoft.EntityFrameworkCore;
using Xunit;
using Persone = ClassLibrary.EFCore.Tests.Entities.Person;

namespace ClassLibrary.EFCore.Tests;

/// <summary>
/// Verifies the repository behavior against the in-memory test database.
/// </summary>
/// <remarks>
/// Each test starts from a clean database so the assertions validate repository behavior rather than
/// relying on state left behind by a previous test.
/// </remarks>
public class Tests : InMemoryDbContext
{
    /// <summary>
    /// Verifies that <see cref="Repository{TEntity, TKey}"/> returns every seeded <see cref="Persone"/> entity.
    /// </summary>
    [Fact]
    public async Task GetAllEntitiesAsync()
    {
        using var dbContext = GetDbContext();
        var repository = new Repository<Persone, int>(dbContext);

        await dbContext.Database.EnsureDeletedAsync(TestContext.Current.CancellationToken);
        await dbContext.Database.EnsureCreatedAsync(TestContext.Current.CancellationToken);

        var entities = await repository.GetAllAsync(cancellationToken: TestContext.Current.CancellationToken);

        Assert.NotNull(entities);
    }

    /// <summary>
    /// Verifies that filtered retrieval only returns <see cref="Persone"/> entities matching the predicate.
    /// </summary>
    [Fact]
    public async Task GetAllEntitiesWithFilterAsync()
    {
        using var dbContext = GetDbContext();
        var repository = new Repository<Persone, int>(dbContext);

        await dbContext.Database.EnsureDeletedAsync(TestContext.Current.CancellationToken);
        await dbContext.Database.EnsureCreatedAsync(TestContext.Current.CancellationToken);

        var entities = await repository.GetAllAsync(includes: null!, filter: x => x.Id >= 3 && x.Id <= 8, cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(6, entities.Count());
        Assert.NotNull(entities);
    }

    /// <summary>
    /// Verifies that <see cref="Repository{TEntity, TKey}.GetByIdAsync(TKey, CancellationToken)"/> returns the expected entity when the key exists.
    /// </summary>
    [Fact]
    public async Task GetEntityByIdAsync()
    {
        using var dbContext = GetDbContext();
        var repository = new Repository<Persone, int>(dbContext);

        await dbContext.Database.EnsureDeletedAsync(TestContext.Current.CancellationToken);
        await dbContext.Database.EnsureCreatedAsync(TestContext.Current.CancellationToken);

        var entity = await repository.GetByIdAsync(2, TestContext.Current.CancellationToken);

        Assert.NotNull(entity);
        Assert.Equal(2, Assert.IsType<Persone>(entity).Id);
        Assert.Equal(entity.Nome, Assert.IsType<Persone>(entity).Nome);
        Assert.Equal(entity.Cognome, Assert.IsType<Persone>(entity).Cognome);
    }

    /// <summary>
    /// Verifies that <see cref="Repository{TEntity, TKey}.GetByIdAsync(TKey, CancellationToken)"/> returns <see langword="null" /> when the key does not exist.
    /// </summary>
    [Fact]
    public async Task GetEntityByIdNotFoundAsync()
    {
        using var dbContext = GetDbContext();
        var repository = new Repository<Persone, int>(dbContext);

        await dbContext.Database.EnsureDeletedAsync(TestContext.Current.CancellationToken);
        await dbContext.Database.EnsureCreatedAsync(TestContext.Current.CancellationToken);

        var entity = await repository.GetByIdAsync(30, TestContext.Current.CancellationToken);

        Assert.Null(entity);
    }

    /// <summary>
    /// Verifies that <see cref="Repository{TEntity, TKey}.CreateAsync(TEntity, CancellationToken)"/> persists a new entity with generated values.
    /// </summary>
    [Fact]
    public async Task CreateEntityAsync()
    {
        using var dbContext = GetDbContext();
        var repository = new Repository<Persone, int>(dbContext);

        await dbContext.Database.EnsureDeletedAsync(TestContext.Current.CancellationToken);
        await dbContext.Database.EnsureCreatedAsync(TestContext.Current.CancellationToken);

        var personFaker = new Faker<Persone>("it")
            .RuleFor(p => p.Id, f => f.IndexFaker + f.Random.Number(11, 100))
            .RuleFor(p => p.Cognome, f => f.Person.LastName)
            .RuleFor(p => p.Nome, f => f.Person.FirstName)
            .RuleFor(p => p.IndirizzoId, f => f.Random.Int(1, 10));

        var entity = personFaker.Generate();

        await repository.CreateAsync(entity, TestContext.Current.CancellationToken);

        Assert.NotNull(entity);
        Assert.True(entity.Id > 0);
        Assert.True(entity.Id <= 100);
        Assert.False(string.IsNullOrEmpty(entity.Nome));
        Assert.False(string.IsNullOrEmpty(entity.Cognome));
    }

    /// <summary>
    /// Verifies that <see cref="Repository{TEntity, TKey}.UpdateAsync(TEntity, CancellationToken)"/> persists changes made to an existing entity.
    /// </summary>
    [Fact]
    public async Task UpdateEntityAsync()
    {
        using var dbContext = GetDbContext();
        var repository = new Repository<Persone, int>(dbContext);

        await dbContext.Database.EnsureDeletedAsync(TestContext.Current.CancellationToken);
        await dbContext.Database.EnsureCreatedAsync(TestContext.Current.CancellationToken);

        var entity = await repository.GetByIdAsync(2, TestContext.Current.CancellationToken);

        if (entity == null)
        {
            return;
        }

        var personFaker = new Faker<Persone>("it")
            .RuleFor(p => p.Cognome, f => f.Person.LastName)
            .RuleFor(p => p.Nome, f => f.Person.FirstName);

        var newEntity = personFaker.Generate();

        entity.Nome = newEntity.Nome;
        entity.Cognome = newEntity.Cognome;

        await repository.UpdateAsync(entity, TestContext.Current.CancellationToken);

        Assert.NotNull(entity);
        Assert.Equal(2, Assert.IsType<Persone>(entity).Id);
        Assert.Equal(newEntity.Nome, Assert.IsType<Persone>(entity).Nome);
        Assert.Equal(newEntity.Cognome, Assert.IsType<Persone>(entity).Cognome);
    }

    /// <summary>
    /// Verifies that <see cref="Repository{TEntity, TKey}.DeleteAsync(TEntity, CancellationToken)"/> removes the specified entity.
    /// </summary>
    [Fact]
    public async Task DeleteEntityAsync()
    {
        using var dbContext = GetDbContext();
        var repository = new Repository<Persone, int>(dbContext);

        await dbContext.Database.EnsureDeletedAsync(TestContext.Current.CancellationToken);
        await dbContext.Database.EnsureCreatedAsync(TestContext.Current.CancellationToken);

        var entity = await repository.GetByIdAsync(4, TestContext.Current.CancellationToken);
        await repository.DeleteAsync(entity!, TestContext.Current.CancellationToken);

        var entities = await repository.GetAllAsync(cancellationToken: TestContext.Current.CancellationToken);

        Assert.NotNull(entities);
        Assert.Equal(9, entities.Count());
    }

    /// <summary>
    /// Verifies that <see cref="Repository{TEntity, TKey}.DeleteByIdAsync(TKey, CancellationToken)"/> removes an entity by its key.
    /// </summary>
    [Fact]
    public async Task DeleteByIdEntityAsync()
    {
        using var dbContext = GetDbContext();
        var repository = new Repository<Persone, int>(dbContext);

        await dbContext.Database.EnsureDeletedAsync(TestContext.Current.CancellationToken);
        await dbContext.Database.EnsureCreatedAsync(TestContext.Current.CancellationToken);

        await repository.DeleteByIdAsync(4, TestContext.Current.CancellationToken);

        var entities = await repository.GetAllAsync(cancellationToken: TestContext.Current.CancellationToken);

        Assert.NotNull(entities);
        Assert.Equal(9, entities.Count());
    }

    /// <summary>
    /// Verifies that paged retrieval returns the expected page when filtering and eager-loading related data.
    /// </summary>
    [Fact]
    public async Task GetPaginatedEntitiesAsync()
    {
        using var dbContext = GetDbContext();
        var repository = new Repository<Persone, int>(dbContext);

        await dbContext.Database.EnsureDeletedAsync(TestContext.Current.CancellationToken);
        await dbContext.Database.EnsureCreatedAsync(TestContext.Current.CancellationToken);

        var entities = await repository.GetAllPagingAsync(pageNumber: 2, pageSize: 5, includes: q => q.Include(p => p.Indirizzo), filter: w => w.Id <= 10, cancellationToken: TestContext.Current.CancellationToken);
        var itemCount = entities.Items.Count;

        Assert.NotNull(entities);
        Assert.Equal(5, itemCount);
        Assert.Contains(entities.Items, x => x.Id == 8);
    }

    /// <summary>
    /// Verifies that paged retrieval works without a filter when ordering is supplied explicitly.
    /// </summary>
    [Fact]
    public async Task GetPaginatedEntitiesWithoutWhereAsync()
    {
        using var dbContext = GetDbContext();
        var repository = new Repository<Persone, int>(dbContext);

        await dbContext.Database.EnsureDeletedAsync(TestContext.Current.CancellationToken);
        await dbContext.Database.EnsureCreatedAsync(TestContext.Current.CancellationToken);

        var entities = await repository.GetAllPagingAsync(pageNumber: 1, pageSize: 5, includes: q => q.Include(p => p.Indirizzo), orderBy: x => x.Id, cancellationToken: TestContext.Current.CancellationToken);
        var itemCount = entities.Items.Count;

        Assert.NotNull(entities);
        Assert.Equal(5, itemCount);
        Assert.Contains(entities.Items, x => x.Id == 3);
    }

    /// <summary>
    /// Verifies that paged retrieval honors descending ordering.
    /// </summary>
    [Fact]
    public async Task GetPaginatedEntitiesDescendingOrderTypeAsync()
    {
        using var dbContext = GetDbContext();
        var repository = new Repository<Persone, int>(dbContext);

        await dbContext.Database.EnsureDeletedAsync(TestContext.Current.CancellationToken);
        await dbContext.Database.EnsureCreatedAsync(TestContext.Current.CancellationToken);

        var entities = await repository.GetAllPagingAsync(pageNumber: 1, pageSize: 5, includes: q => q.Include(p => p.Indirizzo), orderBy: x => x.Id, ascending: false, cancellationToken: TestContext.Current.CancellationToken);
        var itemCount = entities.Items.Count;

        Assert.NotNull(entities);
        Assert.Equal(5, itemCount);
        Assert.Equal(10, Assert.IsType<Persone>(entities.Items.First()).Id);
        Assert.Contains(entities.Items, x => x.Id == 8);
    }

    /// <summary>
    /// Verifies that paged retrieval returns the expected total count and page contents.
    /// </summary>
    [Fact]
    public async Task GetPagingEntitiesAsync()
    {
        using var dbContext = GetDbContext();
        var repository = new Repository<Persone, int>(dbContext);

        await dbContext.Database.EnsureDeletedAsync(TestContext.Current.CancellationToken);
        await dbContext.Database.EnsureCreatedAsync(TestContext.Current.CancellationToken);

        var result = await repository.GetAllPagingAsync(pageNumber: 2, pageSize: 5, includes: q => q.Include(p => p.Indirizzo), filter: w => w.Id <= 10, cancellationToken: TestContext.Current.CancellationToken);

        Assert.NotNull(result);
        Assert.Equal(10, result.TotalItems);
        Assert.Equal(5, result.Items.Count);
        Assert.Contains(result.Items, x => x.Id == 8);
    }
}