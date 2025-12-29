namespace Corely.DataAccess.Interfaces.Entities;

public interface IHasGeneratedIdPk<TKey>
{
    TKey Id { get; set; }
}
