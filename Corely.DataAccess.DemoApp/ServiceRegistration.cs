using Corely.DataAccess.Demo;
using Corely.DataAccess.Demo.Configurations;
using Corely.DataAccess.EntityFramework.Configurations;
using Corely.DataAccess.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Corely.DataAccess.DemoApp;

internal static class ServiceRegistration
{
    public static IServiceProvider GetServiceProvider()
    {
        var services = new ServiceCollection();
        services.AddLogging(b => b.AddConsole().SetMinimumLevel(LogLevel.Debug));
        // Use SQLite in-memory (shared) to demonstrate transactional UoW behavior
        services.AddSingleton<IEFConfiguration>(_ => new SqliteDemoConfiguration());
        services.RegisterEntityFrameworkReposAndUoW();
        services.AddScoped<DemoDbContext>();
        services.AddScoped<DemoDbContext2>();
        services.AddScoped<DemoService>();
        services.AddScoped<DemoService2>();
        var provider = services.BuildServiceProvider();

        // Ensure schema exists for SQLite
        using (var scope = provider.CreateScope())
        {
            var ctx1 = scope.ServiceProvider.GetRequiredService<DemoDbContext>();
            var ctx2 = scope.ServiceProvider.GetRequiredService<DemoDbContext2>();
            ctx1.Database.EnsureCreated();
            ctx2.Database.EnsureCreated();
        }

        return provider;
    }
}
