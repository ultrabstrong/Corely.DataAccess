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

        services.AddScoped<DemoService>();
        services.AddScoped<DemoService2>();

        services.RegisterEFServices();

        return services.BuildServiceProvider();
    }

    private static void RegisterEFServices(this IServiceCollection services)
    {
        var context1Config = new InMemoryDemoConfiguration("DemoDbContext1");

        var context2Config = context1Config;

        services.AddKeyedSingleton<IEFConfiguration>(
            ContextConfigurationKeys.CONTEXT_1_CONFIG,
            context1Config
        );
        services.AddKeyedSingleton<IEFConfiguration>(
            ContextConfigurationKeys.CONTEXT_2_CONFIG,
            context2Config
        );

        services.RegisterEntityFrameworkReposAndUoW();
        services.AddScoped<DemoDbContext>();
        services.AddScoped<DemoDbContext2>();

        services.EnsureSchemasForTestingOnly();
    }
}
