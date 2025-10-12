using AutoFixture;
using Corely.DataAccess.EntityFramework.Configurations;
using Microsoft.EntityFrameworkCore;

namespace Corely.DataAccess.UnitTests.EntityFramework.Configurations;

public class EFSqliteConfigurationBaseTests : DbContextBaseTests
{
    private class MockSqliteEFConfiguration : EFSqliteConfigurationBase
    {
        public MockSqliteEFConfiguration(string connectionString)
            : base(connectionString) { }

        public override void Configure(DbContextOptionsBuilder optionsBuilder) { }
    }

    private readonly Fixture _fixture = new();
    private readonly MockSqliteEFConfiguration _mockSqliteEFConfiguration;

    public EFSqliteConfigurationBaseTests()
    {
        _mockSqliteEFConfiguration = new(_fixture.Create<string>());
    }

    protected override IEFConfiguration EFConfiguration => _mockSqliteEFConfiguration;
}
