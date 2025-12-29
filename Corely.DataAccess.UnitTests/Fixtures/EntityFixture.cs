using Corely.DataAccess.Interfaces.Entities;

namespace Corely.DataAccess.UnitTests.Fixtures;

public class EntityFixture : IHasGeneratedIdPk<int>, IHasCreatedUtc, IHasModifiedUtc
{
    public int Id { get; set; }
    public DateTime CreatedUtc { get; set; }
    public DateTime? ModifiedUtc { get; set; }
    public virtual NavigationPropertyFixture? NavigationProperty { get; set; }
}

public class NavigationPropertyFixture : IHasGeneratedIdPk<int>, IHasCreatedUtc, IHasModifiedUtc
{
    public int Id { get; set; }
    public DateTime CreatedUtc { get; set; }
    public DateTime? ModifiedUtc { get; set; }
}
