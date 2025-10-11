namespace Corely.DataAccess.EntityFramework.UnitOfWork;

internal interface IEFScopeContextSetter
{
    void SetScope(EFUnitOfWorkScope scope);
}
