using Corely.DataAccess.Interfaces.Repos;

namespace Corely.DataAccess.Demo;

public class DemoService(IRepo<DemoEntity> entityRepo)
{
    public Task<List<DemoEntity>> GetAllAsync(CancellationToken ct = default) =>
        entityRepo.ListAsync(cancellationToken: ct);
}
