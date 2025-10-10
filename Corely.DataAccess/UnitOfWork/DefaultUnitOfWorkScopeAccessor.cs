using Corely.DataAccess.Interfaces.UnitOfWork;

namespace Corely.DataAccess.UnitOfWork;

internal sealed class DefaultUnitOfWorkScopeAccessor : IUnitOfWorkScopeAccessor
{
    public bool IsActive { get; set; }
}
