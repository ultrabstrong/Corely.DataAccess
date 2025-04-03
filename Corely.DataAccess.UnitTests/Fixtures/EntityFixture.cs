﻿using Corely.DataAccess.Interfaces.Entities;

namespace Corely.DataAccess.UnitTests.Fixtures;

public class EntityFixture : IHasIdPk<int>, IHasCreatedUtc, IHasModifiedUtc
{
    public int Id { get; set; }
    public DateTime CreatedUtc { get; set; }
    public DateTime? ModifiedUtc { get; set; }
    public virtual NavigationPropertyFixture? NavigationProperty { get; set; }
}

public class NavigationPropertyFixture : IHasIdPk<int>, IHasCreatedUtc, IHasModifiedUtc
{
    public int Id { get; set; }
    public DateTime CreatedUtc { get; set; }
    public DateTime? ModifiedUtc { get; set; }
}
