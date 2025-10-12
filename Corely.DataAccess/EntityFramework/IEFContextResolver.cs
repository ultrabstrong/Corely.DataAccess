namespace Corely.DataAccess.EntityFramework;

internal interface IEFContextResolver
{
    Type GetContextTypeFor(Type entityType);
}
