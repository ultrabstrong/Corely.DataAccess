using Corely.DataAccess.Interfaces.Repos;

namespace Corely.DataAccess.Demo;

internal class DemoService2(IRepo<DemoEntity2> entityRepo)
{
    public Task<List<DemoEntity2>> GetAllAsync(CancellationToken ct = default) =>
        entityRepo.ListAsync(cancellationToken: ct);
}
