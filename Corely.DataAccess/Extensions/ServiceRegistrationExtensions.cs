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
        services.TryAddSingleton(TimeProvider.System);
        services.TryAddSingleton<IEFContextResolver>(sp => new EFContextResolver(sp));
        services.TryAddScoped(typeof(EFReadonlyRepo<,>), typeof(EFReadonlyRepo<,>));
        services.TryAddScoped(typeof(EFRepo<,>), typeof(EFRepo<,>));
        services.TryAddScoped(typeof(IReadonlyRepo<>), typeof(EFReadonlyRepoAdapter<>));
        services.TryAddScoped(typeof(IRepo<>), typeof(EFRepoAdapter<>));

        // Concrete registration: EFRepo injects EFUoWProvider; the interface forwards to the same instance.
        services.TryAddScoped<EFUoWProvider>();
        services.TryAddScoped<IUnitOfWorkProvider>(sp => sp.GetRequiredService<EFUoWProvider>());
        return services;
    }

    public static IServiceCollection RegisterMockReposAndUoW(this IServiceCollection services)
    {
        services.TryAddSingleton(TimeProvider.System);
        services.TryAddScoped(typeof(IRepo<>), typeof(MockRepo<>));
        services.TryAddScoped(typeof(IReadonlyRepo<>), typeof(MockReadonlyRepo<>));
        services.TryAddScoped<IUnitOfWorkProvider, MockUoWProvider>();
        return services;
    }

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
                    if (!relational.Exists())
                    {
                        ctx.Database.EnsureCreated();
                    }
                    else
                    {
                        try
                        {
                            relational.CreateTables();
                        }
                        catch { }
                    }
                }
                else
                {
                    ctx.Database.EnsureCreated();
                }
            }
            catch
            {
                ctx.Database.EnsureCreated();
            }
        }
    }
}
