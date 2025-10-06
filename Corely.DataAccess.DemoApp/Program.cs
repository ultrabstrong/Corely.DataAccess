using Corely.DataAccess.Demo;
using Corely.DataAccess.Interfaces.Repos;
using Corely.DataAccess.Interfaces.UnitOfWork;
using Microsoft.Extensions.DependencyInjection;

namespace Corely.DataAccess.DemoApp;

internal class Program
{
    static async Task Main()
    {
        var provider = ServiceRegistration.GetFullServiceProvider();
        await RepoExample(provider);
        await ReadonlyRepoExample(provider);
        await UnitOfWorkExample(provider);
    }

    static async Task RepoExample(IServiceProvider provider)
    {
        // Repo example with write access. Note repo is a superset of readonly repo
        var repo = provider.GetRequiredService<IRepo<DemoEntity>>();
        if (!await repo.AnyAsync(e => e.Id > 0))
        {
            await repo.CreateAsync(new DemoEntity { Id = 1, Name = "Alpha" });
            await repo.CreateAsync(new DemoEntity { Id = 2, Name = "Beta" });
            await repo.CreateAsync(new DemoEntity { Id = 3, Name = "Gamma" });
        }
        var list = await repo.ListAsync();
        Console.WriteLine($"Entities: {string.Join(", ", list.Select(e => e.Name))}");

        // Update an entity (key must be set on the instance)
        await repo.UpdateAsync(new DemoEntity { Id = 2, Name = "Beta (updated)" });
        list = await repo.ListAsync();
        Console.WriteLine($"Entities after update: {string.Join(", ", list.Select(e => e.Name))}");

        // Delete an entity
        var toDelete = (await repo.ListAsync(e => e.Name == "Alpha")).FirstOrDefault();
        await repo.DeleteAsync(toDelete!);
        list = await repo.ListAsync();
        Console.WriteLine($"Entities after delete: {string.Join(", ", list.Select(e => e.Name))}");
    }

    static async Task ReadonlyRepoExample(IServiceProvider provider)
    {
        // Readonly repo example. Note readonly repo is subset of repo
        var readonlyRepo = provider.GetRequiredService<IReadonlyRepo<DemoEntity>>();
        var list = await readonlyRepo.ListAsync();
        Console.WriteLine($"Entities: {string.Join(", ", list.Select(e => e.Name))}");

        // Total count (no predicate) and filtered count demo using CountAsync
        var totalCount = await readonlyRepo.CountAsync();
        Console.WriteLine($"Total entity count: {totalCount}");
        var gammaCount = await readonlyRepo.CountAsync(e => e.Name.Contains("Gamma"));
        Console.WriteLine($"Entities with 'Gamma' in name: {gammaCount}");

        // Check existence
        bool exists = await readonlyRepo.AnyAsync(e => e.Name.Contains("Gamma"));
        Console.WriteLine($"Entity with 'Gamma' in name exists: {exists}");

        // Get a single entity
        var entity = await readonlyRepo.GetAsync(e => e.Name.Contains("Beta"));
        Console.WriteLine($"Retrieved entity: {entity?.Name}");
    }

    static async Task UnitOfWorkExample(IServiceProvider provider)
    {
        // Unit of Work example with transaction support
        var uowProvider = provider.GetRequiredService<IUnitOfWorkProvider>();
        bool uowSucceeded = false;
        try
        {
            await uowProvider.BeginAsync();

            // Do work here that needs to be atomic

            await uowProvider.CommitAsync();
            uowSucceeded = true;
        }
        finally
        {
            if (!uowSucceeded)
            {
                await uowProvider.RollbackAsync();
            }
        }
    }
}
