namespace Corely.DataAccess.Interfaces.UnitOfWork;

public interface IUnitOfWorkScopeAccessor
{
    bool IsActive { get; set; }
}
