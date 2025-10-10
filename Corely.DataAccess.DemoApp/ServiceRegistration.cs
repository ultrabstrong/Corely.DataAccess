using Corely.DataAccess.Demo;
using Corely.DataAccess.Demo.Configurations;
using Corely.DataAccess.EntityFramework.Configurations;
using Corely.DataAccess.Extensions;
using Corely.DataAccess.Interfaces.UnitOfWork;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Corely.DataAccess.DemoApp;

internal static class ServiceRegistration
{
    public static IServiceProvider GetServiceProvider()
    {
        var services = new ServiceCollection();
        services.AddLogging(b => b.AddConsole().SetMinimumLevel(LogLevel.Debug));
        services.AddSingleton<IEFConfiguration>(_ => new InMemoryDemoConfiguration("demo-generic-db"));
        services.AddScoped<DemoDbContext>();
        services.AddScoped<DemoDbContext2>();
        services.AddAutoEntityContextMap();
        services.AddScoped<IUnitOfWorkProvider, DemoUoWProvider>();
        services.AddScoped<DemoService>();
        services.AddScoped<DemoService2>();
        return services.BuildServiceProvider();
    }
}
