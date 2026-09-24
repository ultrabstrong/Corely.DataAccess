using System.Linq.Expressions;
using Corely.DataAccess.Mock.Repos;
using Corely.DataAccess.UnitTests.Fixtures;

namespace Corely.DataAccess.UnitTests.Mock.Repos;

public class ExpressionExtensionsTests
{
    [Fact]
    public void SelectedProperty_ReturnsTheAccessedProperty()
    {
        Expression<Func<EntityFixture, int>> selector = e => e.Id;

        Assert.Equal(nameof(EntityFixture.Id), selector.SelectedProperty().Name);
    }

    [Fact]
    public void SelectedProperty_SeesThroughAConversion()
    {
        Expression<Func<EntityFixture, object>> selector = e => e.Id;

        Assert.Equal(nameof(EntityFixture.Id), selector.SelectedProperty().Name);
    }

    [Fact]
    public void SelectedProperty_Throws_WhenNotAPropertyAccess()
    {
        Expression<Func<EntityFixture, int>> selector = e => e.Id + 1;

        var ex = Assert.Throws<ArgumentException>(() => selector.SelectedProperty());
        Assert.Equal("property", ex.ParamName);
    }

    [Fact]
    public void SelectedProperty_Throws_WhenNull()
    {
        Expression<Func<EntityFixture, int>> selector = null!;

        Assert.Throws<ArgumentNullException>(() => selector.SelectedProperty());
    }
}
