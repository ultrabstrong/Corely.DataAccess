using Corely.DataAccess.Demo;
using Corely.DataAccess.Demo.Configurations;
using Corely.DataAccess.EntityFramework; // EFUoWProvider
using Corely.DataAccess.EntityFramework.Configurations;
using Corely.DataAccess.EntityFramework.Repos;
using Corely.DataAccess.Extensions;
using Corely.DataAccess.Interfaces.Repos;
using Corely.DataAccess.Interfaces.UnitOfWork;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Corely.DataAccess.DemoApp;

internal static class ServiceRegistration
{
    public static IServiceProvider GetServiceProvider()
    {
        var services = new ServiceCollection();
        services.AddLogging(b => b.AddConsole().SetMinimumLevel(LogLevel.Debug));
        services.AddSingleton<IEFConfiguration>(_ => new InMemoryDemoConfiguration("demo-generic-db"));
        services.AddScoped<DemoDbContext>();
        services.AddAutoEntityContextMap();
        services.AddScoped<IUnitOfWorkProvider, DemoUoWProvider>();
        services.AddScoped<ExampleService>();
        return services.BuildServiceProvider();
    }

    public class ExampleService
    {
        private readonly IRepo<DemoEntity> _entityRepo;
        private readonly IUnitOfWorkProvider _uowProvider;
        private readonly ILogger<ExampleService> _logger;

        public ExampleService(IRepo<DemoEntity> entityRepo, IUnitOfWorkProvider uowProvider, ILogger<ExampleService> logger)
        { _entityRepo = entityRepo; _uowProvider = uowProvider; _logger = logger; }

        public Task<List<DemoEntity>> GetAllAsync(CancellationToken ct = default) => _entityRepo.ListAsync(cancellationToken: ct);
    }
}
