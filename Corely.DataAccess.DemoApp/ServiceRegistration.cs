using Corely.DataAccess.Demo;
using Corely.DataAccess.Demo.Configurations;
using Corely.DataAccess.EntityFramework; // EFUoWProvider
using Corely.DataAccess.EntityFramework.Configurations;
using Corely.DataAccess.EntityFramework.Repos; // minimal example
using Corely.DataAccess.Interfaces.Repos;
using Corely.DataAccess.Interfaces.UnitOfWork;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Corely.DataAccess.DemoApp;

internal static class ServiceRegistration
{
    // Minimal registration example (single DbContext, no custom subclasses)
    // Required pieces:
    // 1. Entity class(es) (e.g. DemoEntity) – POCO, optionally implement IHasIdPk<TKey>, IHasCreatedUtc, IHasModifiedUtc
    // 2. (Optional) Entity configuration(s) inheriting from EntityConfigurationBase<T> or implementing IEntityTypeConfiguration<T>
    // 3. An IEFConfiguration implementation for chosen provider (InMemoryDemoConfiguration/MySqlDemoConfiguration/PostgresDemoConfiguration or custom)
    // 4. DbContext that accepts IEFConfiguration and applies configurations (DemoDbContext already does this)
    // 5. DI registrations:
    //    - IEFConfiguration singleton
    //    - DbContext (scoped)
    //    - IReadonlyRepo<> -> EFReadonlyRepo<>
    //    - IRepo<> -> EFRepo<>
    //    - (Optional) IUnitOfWorkProvider -> EFUoWProvider for deferred SaveChanges + transactions
    // Usage pattern:
    //    using var scope = provider.CreateScope();
    //    var repo = scope.ServiceProvider.GetRequiredService<IRepo<DemoEntity>>();
    //    await repo.CreateAsync(entity); // auto-saves (outside UoW)
    //    var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWorkProvider>();
    //    await uow.BeginAsync(); await repo.UpdateAsync(entity); await uow.CommitAsync();
    public static IServiceProvider GetMinimalServiceProvider()
    {
        var services = new ServiceCollection();
        services.AddLogging(b => b.AddConsole().SetMinimumLevel(LogLevel.Information));

        // Choose a configuration (InMemory shown here)
        services.AddSingleton<IEFConfiguration>(_ => new InMemoryDemoConfiguration("minimal-db"));

        // Register DbContext
        services.AddScoped<DemoDbContext>();

        // Open generic repository registrations using base EF implementations
        services.AddScoped(typeof(IReadonlyRepo<>), typeof(EFReadonlyRepo<>));
        services.AddScoped(typeof(IRepo<>), typeof(EFRepo<>));

        // Unit of Work provider (optional; enables deferred SaveChanges + transaction support)
        services.AddScoped<IUnitOfWorkProvider, EFUoWProvider>();

        return services.BuildServiceProvider();
    }

    // Full registration example:
    // Everything in minimal, PLUS custom subclassed repositories & UoW (DemoReadonlyRepo<>, DemoRepo<>, DemoUoWProvider)
    // Useful when you want context-specific extensions or multiple DbContexts in an application.
    public static IServiceProvider GetFullServiceProvider()
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
