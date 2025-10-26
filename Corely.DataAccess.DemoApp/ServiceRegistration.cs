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

        // ================= 1. REGISTER LOGGING =================
        services.AddLogging(b => b.AddConsole().SetMinimumLevel(LogLevel.Debug));

        // ================= 2. REGISTER DEMO SERVICES =================
        services.AddScoped<DemoService>();
        services.AddScoped<DemoService2>();

        // ================= 3. REGISTER REPO AND UOW SERVICES =================
        services.RegisterEFServices();
        // Comment the line above and uncomment the line below to use mock repos and UoW instead of EF
        // services.RegisterMockReposAndUoW();

        return services.BuildServiceProvider();
    }

    private static void RegisterEFServices(this IServiceCollection services)
    {
        // ================= REGISTER EF CONFIGURATION (CONNECTION) =================

        // Uncomment to use In-Memory database
        var context1Config = new InMemoryDemoConfiguration("DemoDbContext1");

        // Uncomment to use Sqlite
        //var context1Config = new SqliteDemoConfiguration("Data Source=:memory:;Cache=Shared");

        // Uncomment to use MySQL
        // var context1Config = new MySqlDemoConfiguration("Server=localhost;Port=3306;Database=dataaccessdemo;Uid=root;Pwd=admin;");

        // This is just to demonstrate multiple DbContexts with different configurations
        // Most of the time you will only need one DbContext and one IEFConfiguration
        // Can use a different configuration to test multiple connection scenarios
        var context2Config = context1Config;

        services.AddKeyedSingleton<IEFConfiguration>(
            ContextConfigurationKeys.CONTEXT_1_CONFIG,
            context1Config
        );
        services.AddKeyedSingleton<IEFConfiguration>(
            ContextConfigurationKeys.CONTEXT_2_CONFIG,
            context2Config
        );

        // ================= REGISTER REPOSITORIES, UNIT OF WORK, AND CONTEXTS =================
        services.RegisterEntityFrameworkReposAndUoW();
        services.AddScoped<DemoDbContext>();
        services.AddScoped<DemoDbContext2>();

        // Ensure schema exists for the specified DbContexts (order matters for shared in-memory relational providers)
        // NOTE this is for demo purposes; in a real app, use migrations and proper deployment practices
        services.EnsureSchemasForTestingOnly();
    }
}
