using Corely.DataAccess.Interfaces.Repos;

namespace Corely.DataAccess.Demo;

internal class DemoService(IRepo<DemoEntity> entityRepo)
{
    public Task<List<DemoEntity>> GetAllAsync(CancellationToken ct = default) =>
        entityRepo.ListAsync(cancellationToken: ct);
}
