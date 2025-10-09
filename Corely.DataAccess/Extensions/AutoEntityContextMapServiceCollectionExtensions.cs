using Corely.DataAccess.EntityFramework.Repos;
using Corely.DataAccess.Interfaces.Repos;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Corely.DataAccess.Extensions;

public static class AutoEntityContextMapServiceCollectionExtensions
{
    public static IServiceCollection AddAutoEntityContextMap(this IServiceCollection services)
    {
        var contextTypes = services
            .Where(d => d.ServiceType != null && typeof(DbContext).IsAssignableFrom(d.ServiceType))
            .Select(d => d.ServiceType)
            .Distinct()
            .ToArray();

        // Register consolidated context-qualified repos
        services.AddScoped(typeof(EFReadonlyRepo<,>), typeof(EFReadonlyRepo<,>));
        services.AddScoped(typeof(EFRepo<,>), typeof(EFRepo<,>));

        // Adapters for public single-generic interfaces
        services.AddScoped(typeof(IReadonlyRepo<>), typeof(EFReadonlyRepoAdapter<>));
        services.AddScoped(typeof(IRepo<>), typeof(EFRepoAdapter<>));

        services.AddSingleton<IEntityContextMap>(sp => new AutoEntityContextMap(sp, contextTypes));
        return services;
    }
}
