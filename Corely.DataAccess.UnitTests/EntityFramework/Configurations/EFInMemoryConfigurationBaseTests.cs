﻿using Corely.DataAccess.EntityFramework.Configurations;
using Microsoft.EntityFrameworkCore;

namespace Corely.DataAccess.UnitTests.EntityFramework.Configurations;

public class EFInMemoryConfigurationBaseTests : DbContextBaseTests
{
    private class MockInMemoryEFConfiguration : EFInMemoryConfigurationBase
    {
        public override void Configure(DbContextOptionsBuilder optionsBuilder) { }
    }

    private readonly MockInMemoryEFConfiguration _mockInMemoryEFConfiguration = new();

    protected override IEFConfiguration EFConfiguration => _mockInMemoryEFConfiguration;
}
