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
                    new DemoEntity { Name = "Cat" },
                    new DemoEntity { Name = "Dog" },
                    new DemoEntity { Name = "Fox" },
                ]
            );
        }
        var list = await repo.ListAsync();
        Console.WriteLine($"Entities: {string.Join(", ", list.Select(e => e.Name))}");

        var toUpdate = (await repo.ListAsync(e => e.Name == "Dog")).FirstOrDefault();
        if (toUpdate != null)
        {
            toUpdate.Name = "Dog (updated)";
            await repo.UpdateAsync(toUpdate);
        }
        list = await repo.ListAsync();
        Console.WriteLine($"Entities after update: {string.Join(", ", list.Select(e => e.Name))}");

        var toDelete = (await repo.ListAsync(e => e.Name == "Cat")).FirstOrDefault();
        if (toDelete != null)
            await repo.DeleteAsync(toDelete);
        list = await repo.ListAsync();
        Console.WriteLine($"Entities after delete: {string.Join(", ", list.Select(e => e.Name))}");

        var repo2 = provider.GetRequiredService<IRepo<DemoEntity2>>();
        await repo2.CreateAsync(new DemoEntity2 { Name = "Red" });

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

        bool exists = await readonlyRepo.AnyAsync(e => e.Name.Contains("Fox"));
        Console.WriteLine($"Entity with 'Fox' in name exists: {exists}");

        var count = await readonlyRepo.CountAsync(e => e.Name.Contains("Fox"));
        Console.WriteLine($"Entities with 'Fox' in name: {count}");

        var entity = await readonlyRepo.GetAsync(e => e.Name.Contains("Dog"));
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
        // NOTE : UoW doesn't work with in memory databases (sqlite in-memory included)
        // because they don't support transactions / nested transactions
        Console.WriteLine();
        Console.WriteLine("Unit of Work Example:");

        var repo = provider.GetRequiredService<IRepo<DemoEntity>>();
        var service = provider.GetRequiredService<DemoService>();
        var repo2 = provider.GetRequiredService<IRepo<DemoEntity2>>();
        var uowProvider = provider.GetRequiredService<IUnitOfWorkProvider>();
        try
        {
            await uowProvider.BeginAsync();
            await repo.CreateAsync(new DemoEntity { Name = "Lion" });
            // also works for services using repos
            await service.CreateAsync(new DemoEntity { Name = "Tiger" });
            // also works across multiple DbContexts
            await repo2.CreateAsync(new DemoEntity2 { Name = "Cyan" });
            throw new Exception();
        }
        catch
        {
            await uowProvider.RollbackAsync();
        }

        var context1EntitiesAfterRollback = await repo.ListAsync();
        Console.WriteLine(
            $"Context 1 entities after rollback: {string.Join(", ", context1EntitiesAfterRollback.Select(e => e.Name))}"
        );

        var context2EntitiesAfterRollback = await repo2.ListAsync();
        Console.WriteLine(
            $"Context 2 entities after rollback: {string.Join(", ", context2EntitiesAfterRollback.Select(e => e.Name))}"
        );

        try
        {
            await uowProvider.BeginAsync();
            await repo.CreateAsync(new DemoEntity { Name = "Lion" });
            // also works for services using repos
            await service.CreateAsync(new DemoEntity { Name = "Tiger" });
            // also works across multiple DbContexts
            await repo2.CreateAsync(new DemoEntity2 { Name = "Cyan" });
            await uowProvider.CommitAsync();
        }
        catch
        {
            await uowProvider.RollbackAsync();
        }

        var context1EntitiesAfterCommit = await repo.ListAsync();
        Console.WriteLine(
            $"Context 1 entities after commit: {string.Join(", ", context1EntitiesAfterCommit.Select(e => e.Name))}"
        );

        var context2EntitiesAfterCommit = await repo2.ListAsync();
        Console.WriteLine(
            $"Context 2 entities after commit: {string.Join(", ", context2EntitiesAfterCommit.Select(e => e.Name))}"
        );
    }
}
