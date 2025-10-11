using Corely.DataAccess.Demo;
using Corely.DataAccess.Interfaces.Repos;
using Corely.DataAccess.Interfaces.UnitOfWork;
using Microsoft.Extensions.DependencyInjection;

namespace Corely.DataAccess.DemoApp;

internal class Program
{
    static async Task Main()
    {
        var provider = ServiceRegistration.GetServiceProvider();
        await RepoExample(provider);
        await ReadonlyRepoExample(provider);
        await MultipleServiceExample(provider);
        await UnitOfWorkExample(provider);
    }

    static async Task RepoExample(IServiceProvider provider)
    {
        Console.WriteLine("Repo Example:");

        var repo = provider.GetRequiredService<IRepo<DemoEntity>>();
        if (!await repo.AnyAsync(e => e.Id > 0))
        {
            await repo.CreateAsync(new DemoEntity { Id = 1, Name = "Alpha" });
            await repo.CreateAsync(new DemoEntity { Id = 2, Name = "Beta" });
            await repo.CreateAsync(new DemoEntity { Id = 3, Name = "Gamma" });
        }
        var list = await repo.ListAsync();
        Console.WriteLine($"Entities: {string.Join(", ", list.Select(e => e.Name))}");

        await repo.UpdateAsync(new DemoEntity { Id = 2, Name = "Beta (updated)" });
        list = await repo.ListAsync();
        Console.WriteLine($"Entities after update: {string.Join(", ", list.Select(e => e.Name))}");

        var toDelete = (await repo.ListAsync(e => e.Name == "Alpha")).FirstOrDefault();
        await repo.DeleteAsync(toDelete!);
        list = await repo.ListAsync();
        Console.WriteLine($"Entities after delete: {string.Join(", ", list.Select(e => e.Name))}");
    }

    static async Task ReadonlyRepoExample(IServiceProvider provider)
    {
        Console.WriteLine();
        Console.WriteLine("Readonly Repo Example:");

        var readonlyRepo = provider.GetRequiredService<IReadonlyRepo<DemoEntity>>();
        var list = await readonlyRepo.ListAsync();
        Console.WriteLine($"Entities: {string.Join(", ", list.Select(e => e.Name))}");

        var totalCount = await readonlyRepo.CountAsync();
        Console.WriteLine($"Total entity count: {totalCount}");
        var gammaCount = await readonlyRepo.CountAsync(e => e.Name.Contains("Gamma"));
        Console.WriteLine($"Entities with 'Gamma' in name: {gammaCount}");

        bool exists = await readonlyRepo.AnyAsync(e => e.Name.Contains("Gamma"));
        Console.WriteLine($"Entity with 'Gamma' in name exists: {exists}");

        var entity = await readonlyRepo.GetAsync(e => e.Name.Contains("Beta"));
        Console.WriteLine($"Retrieved entity: {entity?.Name}");
    }

    static async Task MultipleServiceExample(IServiceProvider provider)
    {
        Console.WriteLine();
        Console.WriteLine("Multiple Service Example:");

        var service1 = provider.GetRequiredService<DemoService>();
        var entities1 = await service1.GetAllAsync();
        Console.WriteLine($"Service1 Entities: {string.Join(", ", entities1.Select(e => e.Name))}");

        var service2 = provider.GetRequiredService<DemoService2>();
        var entities2 = await service2.GetAllAsync();
        Console.WriteLine($"Service2 Entities: {string.Join(", ", entities2.Select(e => e.Name))}");
    }

    static async Task UnitOfWorkExample(IServiceProvider provider)
    {
        Console.WriteLine();
        Console.WriteLine("Unit of Work Example:");

        var unscopedRepo = provider.GetRequiredService<IRepo<DemoEntity>>();

        var uowProvider = provider.GetRequiredService<IUnitOfWorkProvider>();
        bool uowSucceeded = false;
        try
        {
            await uowProvider.BeginAsync();

            // Note: the UoW provider supports repos from multiple contexts
            // if the underlying database supports nested transactions.
            var scopedRepo = uowProvider.GetRepository<DemoEntity>();
            await scopedRepo.CreateAsync(new DemoEntity { Id = 4, Name = "Delta" });

            var entitiesBeforeCommit = await unscopedRepo.ListAsync();
            Console.WriteLine(
                $"Entities from Unscoped Repo before UoW commit: {string.Join(", ", entitiesBeforeCommit.Select(e => e.Name))}"
            );

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

        var entitiesAfterCommit = await unscopedRepo.ListAsync();
        Console.WriteLine(
            $"Entities from Unscoped Repo after UoW commit: {string.Join(", ", entitiesAfterCommit.Select(e => e.Name))}"
        );
    }
}
