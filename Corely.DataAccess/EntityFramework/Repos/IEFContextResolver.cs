namespace Corely.DataAccess.EntityFramework.Repos;

internal interface IEFContextResolver
{
    Type GetContextTypeFor(Type entityType);
}
