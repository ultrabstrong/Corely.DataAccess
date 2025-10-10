using Corely.DataAccess.Interfaces.Repos;
using Corely.DataAccess.Interfaces.UnitOfWork;
using Microsoft.Extensions.Logging;

namespace Corely.DataAccess.Demo;
public class DemoService(IRepo<DemoEntity> entityRepo, IUnitOfWorkProvider uowProvider, ILogger<DemoService> logger)
{
    public Task<List<DemoEntity>> GetAllAsync(CancellationToken ct = default) => entityRepo.ListAsync(cancellationToken: ct);
}
