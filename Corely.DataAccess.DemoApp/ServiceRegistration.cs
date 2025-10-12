using Corely.DataAccess.Demo;
using Corely.DataAccess.Demo.Configurations;
using Corely.DataAccess.EntityFramework.Configurations;
using Corely.DataAccess.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
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

        // ================= 2. REGISTER EF CONFIGURATION (CONNECTION) =================

        // Uncomment to use In-Memory database
        var context1Config = new InMemoryDemoConfiguration("DemoDbContext1");

        // Uncomment to use Sqlite
        // var context1Config = new SqliteDemoConfiguration("Data Source=:memory:;Cache=Shared");

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

        // ================= 3. REGISTER REPOSITORIES, UNIT OF WORK, AND CONTEXTS =================
        services.RegisterEntityFrameworkReposAndUoW();
        services.AddScoped<DemoDbContext>();
        services.AddScoped<DemoDbContext2>();

        // ================= 4. REGISTER DEMO SERVICES =================
        services.AddScoped<DemoService>();
        services.AddScoped<DemoService2>();

        var provider = services.BuildServiceProvider();

        // Ensure schema exists for the specified DbContexts (order matters for shared in-memory relational providers)
        // NOTE this is for demo purposes; in a real app, use migrations and proper deployment practices
        EnsureSchemas(provider, typeof(DemoDbContext), typeof(DemoDbContext2));

        return provider;
    }

    /*
     * Demo-only method to ensure schemas exist for the specified DbContext types.
     * Works whether contexts share the same database/connection or use different ones.
     * In a real application, use migrations and proper deployment practices.
     */
    private static void EnsureSchemas(IServiceProvider provider, params Type[] dbContextTypes)
    {
        using var scope = provider.CreateScope();
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
