using Corely.DataAccess.Demo;
using Corely.DataAccess.Demo.Configurations;
using Corely.DataAccess.EntityFramework.Configurations;
using Corely.DataAccess.Interfaces.Repos;
using Corely.DataAccess.Interfaces.UnitOfWork;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Corely.DataAccess.DemoApp;

internal static class ServiceRegistration
{
    public static IServiceProvider GetDemoServiceProvider()
    {
        var services = new ServiceCollection();

        services.AddLogging(b => b.AddConsole().SetMinimumLevel(LogLevel.Debug));

        // Default: InMemory demo configuration
        services.AddSingleton<IEFConfiguration>(_ => new InMemoryDemoConfiguration("demo-generic-db"));
        // services.AddSingleton<IEFConfiguration>(_ => new MySqlDemoConfiguration("Server=localhost;Port=3306;Database=demo;User=root;Password=Password123!;"));
        // services.AddSingleton<IEFConfiguration>(_ => new PostgresDemoConfiguration("Host=localhost;Port=5432;Database=demo;Username=postgres;Password=Password123!"));

        services.AddScoped<DemoDbContext>();

        // Repo and ReadonlyRepo for interactions that don't need UoW support
        services.AddScoped(typeof(IReadonlyRepo<>), typeof(DemoReadonlyRepo<>));
        services.AddScoped(typeof(IRepo<>), typeof(DemoRepo<>));
        // UoW provider for interactions that need UoW (transaction) support
        services.AddScoped<IUnitOfWorkProvider, DemoUoWProvider>();

        return services.BuildServiceProvider();
    }
}
