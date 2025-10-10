using Corely.DataAccess.EntityFramework;
using Corely.DataAccess.EntityFramework.Repos;
using Corely.DataAccess.Interfaces.Repos;
using Corely.DataAccess.Interfaces.UnitOfWork;
using Corely.DataAccess.UnitOfWork;
using Microsoft.Extensions.DependencyInjection;

namespace Corely.DataAccess.Extensions;

public static class ServiceRegistrationExtensions
{
    public static IServiceCollection AutoRegisterEntityFrameworkProviders(this IServiceCollection services)
    {
        services.AddSingleton<IEntityContextMap>(sp => new AutoEntityContextMap(sp));

        services.AddScoped(typeof(EFReadonlyRepo<,>), typeof(EFReadonlyRepo<,>));
        services.AddScoped(typeof(EFRepo<,>), typeof(EFRepo<,>));
        services.AddScoped(typeof(IReadonlyRepo<>), typeof(EFReadonlyRepoAdapter<>));
        services.AddScoped(typeof(IRepo<>), typeof(EFRepoAdapter<>));

        services.AddScoped<IUnitOfWorkScopeAccessor, DefaultUnitOfWorkScopeAccessor>();
        services.AddScoped<IUnitOfWorkProvider, EFUoWProvider>();
        return services;
    }
}
