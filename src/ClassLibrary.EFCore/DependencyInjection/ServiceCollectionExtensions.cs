using Microsoft.Extensions.DependencyInjection;

namespace ClassLibrary.EFCore.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddRepositoryRegistration<TDbContext>(this IServiceCollection services) where TDbContext : DbContext
    {
        services
            .AddScoped<DbContext, TDbContext>()
            .AddScoped(typeof(IRepository<,>), typeof(Repository<,>));

        return services;
    }
}