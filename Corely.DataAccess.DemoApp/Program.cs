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
            await repo.CreateAsync(
                [
                    new DemoEntity { Name = "Alpha" },
                    new DemoEntity { Name = "Beta" },
                    new DemoEntity { Name = "Gamma" },
                ]
            );
        }
        var list = await repo.ListAsync();
        Console.WriteLine($"Entities: {string.Join(", ", list.Select(e => e.Name))}");

        var toUpdate = (await repo.ListAsync(e => e.Name == "Beta")).FirstOrDefault();
        if (toUpdate != null)
        {
            toUpdate.Name = "Beta (updated)";
            await repo.UpdateAsync(toUpdate);
        }
        list = await repo.ListAsync();
        Console.WriteLine($"Entities after update: {string.Join(", ", list.Select(e => e.Name))}");

        var toDelete = (await repo.ListAsync(e => e.Name == "Alpha")).FirstOrDefault();
        if (toDelete != null)
            await repo.DeleteAsync(toDelete);
        list = await repo.ListAsync();
        Console.WriteLine($"Entities after delete: {string.Join(", ", list.Select(e => e.Name))}");

        var repo2 = provider.GetRequiredService<IRepo<DemoEntity2>>();
        await repo2.CreateAsync(new DemoEntity2 { Name = "Entity2 - One" });

        var list2 = await repo2.ListAsync();
        Console.WriteLine(
            $"Entities in second repo: {string.Join(", ", list2.Select(e => e.Name))}"
        );
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

        var repo = provider.GetRequiredService<IRepo<DemoEntity>>();
        var service = provider.GetRequiredService<DemoService>();
        var uowProvider = provider.GetRequiredService<IUnitOfWorkProvider>();
        try
        {
            await uowProvider.BeginAsync();

            await repo.CreateAsync(new DemoEntity { Name = "fromRepo" });
            // also works for services using repos
            await service.CreateAsync(new DemoEntity { Name = "fromService" });

            var entitiesBeforeCommit = await repo.ListAsync();
            Console.WriteLine(
                $"Entities before UoW commit: {string.Join(", ", entitiesBeforeCommit.Select(e => e.Name))}"
            );
            //throw new Exception(); // uncomment to throw and see rollback functionality
            await uowProvider.CommitAsync();
        }
        catch
        {
            await uowProvider.RollbackAsync();
        }

        var entitiesAfterCommit = await repo.ListAsync();
        Console.WriteLine(
            $"Entities after UoW commit: {string.Join(", ", entitiesAfterCommit.Select(e => e.Name))}"
        );
    }
}
