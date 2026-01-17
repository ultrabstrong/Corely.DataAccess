using AutoFixture;
using Corely.DataAccess.EntityFramework.Configurations;
using Microsoft.EntityFrameworkCore;

namespace Corely.DataAccess.UnitTests.EntityFramework.Configurations;

public class EFMsSqlConfigurationBaseTests : EFConfigurationTestsBase
{
    private class MockMsSqlEFConfiguration : EFMsSqlConfigurationBase
    {
        public MockMsSqlEFConfiguration(string connectionString)
            : base(connectionString) { }

        public override void Configure(DbContextOptionsBuilder optionsBuilder) { }
    }

    private readonly Fixture _fixture = new();
    private readonly MockMsSqlEFConfiguration _mockMsSqlEFConfiguration;

    public EFMsSqlConfigurationBaseTests()
    {
        _mockMsSqlEFConfiguration = new(_fixture.Create<string>());
    }

    protected override IEFConfiguration EFConfiguration => _mockMsSqlEFConfiguration;
}
