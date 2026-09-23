using Corely.DataAccess.Interfaces.Entities;

namespace Corely.DataAccess.Demo;

internal class DemoEntity : IHasGeneratedIdPk<int>, IHasCreatedUtc, IHasModifiedUtc
{
    public string Name { get; set; } = string.Empty;

    public int Id { get; set; }
    public DateTime CreatedUtc { get; set; }
    public DateTime? ModifiedUtc { get; set; }
}

internal class DemoEntity2 : IHasGeneratedIdPk<int>, IHasCreatedUtc, IHasModifiedUtc
{
    public string Name { get; set; } = string.Empty;

    public int Id { get; set; }
    public DateTime CreatedUtc { get; set; }
    public DateTime? ModifiedUtc { get; set; }
}
