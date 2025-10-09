namespace Corely.DataAccess.Interfaces.Repos;
public interface IEntityContextMap
{
    Type GetContextTypeFor(Type entityType);
}
