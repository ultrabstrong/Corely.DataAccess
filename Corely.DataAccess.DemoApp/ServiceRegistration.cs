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
        // Use In-Memory database
        services.AddSingleton<IEFConfiguration>(_ => new InMemoryDemoConfiguration(
            Guid.NewGuid().ToString()
        ));

        // Uncomment to use Sqlite
        /*
        services.AddSingleton<IEFConfiguration>(_ => new SqliteDemoConfiguration(
            "Data Source=:memory:;Cache=Shared"
        ));
        */

        // Uncomment to use MySQL
        /*
        services.AddSingleton<IEFConfiguration>(_ => new MySqlDemoConfiguration(
            "Server=localhost;Port=3306;Database=dataaccessdemo;Uid=root;Pwd=admin;"
        ));
        */

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
     * In a real application, use migrations and proper deployment practices.
     */
    private static void EnsureSchemas(IServiceProvider provider, params Type[] dbContextTypes)
    {
        using var scope = provider.CreateScope();
        DbContext? first = null;
        foreach (var ctxType in dbContextTypes)
        {
            var ctx = (DbContext)scope.ServiceProvider.GetRequiredService(ctxType);
            if (first is null)
            {
                // Create database and this context's tables
                ctx.Database.EnsureCreated();
                first = ctx;
                continue;
            }

            // For additional contexts on an existing relational DB, create their tables explicitly
            // because EnsureCreated is a no-op if the database exists.
            // For non-relational providers (e.g., InMemory), fall back to EnsureCreated.
            try
            {
                var creator = ctx.Database.GetService<IDatabaseCreator>();
                if (creator is IRelationalDatabaseCreator relational)
                {
                    try
                    {
                        relational.CreateTables();
                    }
                    catch
                    { /* ignore if exists */
                    }
                }
                else
                {
                    ctx.Database.EnsureCreated();
                }
            }
            catch
            {
                // As a last resort, attempt EnsureCreated
                ctx.Database.EnsureCreated();
            }
        }
    }
}
