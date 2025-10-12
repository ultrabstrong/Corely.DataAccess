using Corely.DataAccess.EntityFramework;
using Corely.DataAccess.EntityFramework.Repos;
using Corely.DataAccess.EntityFramework.UnitOfWork;
using Corely.DataAccess.Interfaces.Repos;
using Corely.DataAccess.Interfaces.UnitOfWork;
using Corely.DataAccess.Mock;
using Corely.DataAccess.Mock.Repos;
using Microsoft.Extensions.DependencyInjection;

namespace Corely.DataAccess.Extensions;

public static class ServiceRegistrationExtensions
{
    public static IServiceCollection RegisterEntityFrameworkReposAndUoW(
        this IServiceCollection services
    )
    {
        services.AddSingleton<IEFContextResolver>(sp => new EFContextResolver(sp));
        services.AddScoped(typeof(EFReadonlyRepo<,>), typeof(EFReadonlyRepo<,>));
        services.AddScoped(typeof(EFRepo<,>), typeof(EFRepo<,>));
        services.AddScoped(typeof(IReadonlyRepo<>), typeof(EFReadonlyRepoAdapter<>));
        services.AddScoped(typeof(IRepo<>), typeof(EFRepoAdapter<>));

        // Register a single scoped EFUoWProvider instance per scope and expose it via the interface
        services.AddScoped<EFUoWProvider>();
        services.AddScoped<IUnitOfWorkProvider>(sp => sp.GetRequiredService<EFUoWProvider>());
        return services;
    }

    public static IServiceCollection RegisterMockReposAndUoW(this IServiceCollection services)
    {
        services.AddScoped(typeof(IRepo<>), typeof(MockRepo<>));
        services.AddScoped(typeof(IReadonlyRepo<>), typeof(MockReadonlyRepo<>));
        services.AddScoped<IUnitOfWorkProvider, MockUoWProvider>();
        return services;
    }
}
