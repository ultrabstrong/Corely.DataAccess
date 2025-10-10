using Corely.DataAccess.Interfaces.Repos;
using Corely.DataAccess.Interfaces.UnitOfWork;
using Microsoft.Extensions.Logging;

namespace Corely.DataAccess.Demo;
public class DemoService2(IRepo<DemoEntity2> entityRepo, IUnitOfWorkProvider uowProvider, ILogger<DemoService2> logger)
{
    public Task<List<DemoEntity2>> GetAllAsync(CancellationToken ct = default) => entityRepo.ListAsync(cancellationToken: ct);
}
