using Microsoft.Extensions.DependencyInjection;

namespace ClassLibrary.EFCore.DependencyInjection;

/// <summary>
/// Provides extension methods for registering repository services with an <see cref="IServiceCollection"/> in
/// applications using Entity Framework Core.
/// </summary>
/// <remarks>These extension methods simplify the setup of repository patterns by registering the required <see
/// cref="DbContext"/> and generic repository types for dependency injection. Use these methods during application
/// startup to ensure repositories are available for injection throughout the application's lifetime.</remarks>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers the specified Entity Framework Core DbContext and generic repository services with the dependency
    /// injection container.
    /// </summary>
    /// <remarks>This method adds scoped registrations for the specified <typeparamref name="TDbContext"/> as
    /// the application's <see cref="DbContext"/>, and for <see cref="IRepository{TEntity, TKey}"/> as <see
    /// cref="Repository{TEntity, TKey}"/>. This enables consumers to resolve repositories and the DbContext via
    /// dependency injection.</remarks>
    /// <typeparam name="TDbContext">The type of the DbContext to register. Must inherit from <see cref="DbContext"/>.</typeparam>
    /// <param name="services">The <see cref="IServiceCollection"/> to add the services to.</param>
    /// <returns>The same <see cref="IServiceCollection"/> instance so that additional calls can be chained.</returns>
    public static IServiceCollection AddRepositoryRegistration<TDbContext>(this IServiceCollection services) where TDbContext : DbContext
    {
        services
            .AddScoped<DbContext, TDbContext>()
            .AddScoped(typeof(IRepository<,>), typeof(Repository<,>));

        return services;
    }
}