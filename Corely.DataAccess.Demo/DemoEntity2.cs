using Corely.DataAccess.Interfaces.Entities;

namespace Corely.DataAccess.Demo;

internal class DemoEntity2 : IHasIdPk<int>, IHasCreatedUtc, IHasModifiedUtc
{
    // Custom property ; need to configure manually
    public string Name { get; set; } = string.Empty;

    // Inherited properties that are auto-configured
    public int Id { get; set; }
    public DateTime CreatedUtc { get; set; }
    public DateTime? ModifiedUtc { get; set; }
}
