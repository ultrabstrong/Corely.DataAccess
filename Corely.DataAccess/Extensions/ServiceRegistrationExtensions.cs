using Corely.DataAccess.EntityFramework;
using Corely.DataAccess.EntityFramework.Repos;
using Corely.DataAccess.EntityFramework.UnitOfWork;
using Corely.DataAccess.Interfaces.Repos;
using Corely.DataAccess.Interfaces.UnitOfWork;
using Corely.DataAccess.Mock;
using Corely.DataAccess.Mock.Repos;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Corely.DataAccess.Extensions;

public static class ServiceRegistrationExtensions
{
    public static IServiceCollection RegisterEntityFrameworkReposAndUoW(
        this IServiceCollection services
    )
    {
        services.TryAddSingleton<IEFContextResolver>(sp => new EFContextResolver(sp));
        services.TryAddScoped(typeof(EFReadonlyRepo<,>), typeof(EFReadonlyRepo<,>));
        services.TryAddScoped(typeof(EFRepo<,>), typeof(EFRepo<,>));
        services.TryAddScoped(typeof(IReadonlyRepo<>), typeof(EFReadonlyRepoAdapter<>));
        services.TryAddScoped(typeof(IRepo<>), typeof(EFRepoAdapter<>));

        // Register EFUoWProvider as concrete type because EFRepo injects it directly
        // to access EF-specific members (IsActive, Register) not exposed on IUnitOfWorkProvider.
        // The interface registration forwards to the same scoped instance so consumers
        // resolving IUnitOfWorkProvider get the same instance as the repos.
        services.TryAddScoped<EFUoWProvider>();
        services.TryAddScoped<IUnitOfWorkProvider>(sp => sp.GetRequiredService<EFUoWProvider>());
        return services;
    }

    public static IServiceCollection RegisterMockReposAndUoW(this IServiceCollection services)
    {
        services.TryAddScoped(typeof(IRepo<>), typeof(MockRepo<>));
        services.TryAddScoped(typeof(IReadonlyRepo<>), typeof(MockReadonlyRepo<>));
        services.TryAddScoped<IUnitOfWorkProvider, MockUoWProvider>();
        return services;
    }

    /*
     * FOR TESTING ONLY
     * Method to ensure schemas exist for the specified DbContext types.
     * Works whether contexts share the same database/connection or use different ones.
     * In a real application, use migrations and proper deployment practices.
     */
    public static void EnsureSchemasForTestingOnly(this IServiceCollection services)
    {
        var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();

        var dbContextTypes = services
            .Where(sd => typeof(DbContext).IsAssignableFrom(sd.ServiceType))
            .Select(sd => sd.ServiceType)
            .Distinct()
            .ToList();

        foreach (var ctxType in dbContextTypes)
        {
            var ctx = (DbContext)scope.ServiceProvider.GetRequiredService(ctxType);
            try
            {
                var creator = ctx.Database.GetService<IDatabaseCreator>();
                if (creator is IRelationalDatabaseCreator relational)
                {
                    // If the database doesn't exist for this context, create it (and its tables)
                    if (!relational.Exists())
                    {
                        ctx.Database.EnsureCreated();
                    }
                    else
                    {
                        // Database exists; attempt to create tables for this context's model
                        try
                        {
                            relational.CreateTables();
                        }
                        catch
                        {
                            // Ignore if tables already exist or provider throws for existing tables
                        }
                    }
                }
                else
                {
                    // Non-relational providers (e.g., InMemory)
                    ctx.Database.EnsureCreated();
                }
            }
            catch
            {
                // Fallback
                ctx.Database.EnsureCreated();
            }
        }
    }
}
