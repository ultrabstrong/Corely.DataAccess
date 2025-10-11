using Microsoft.EntityFrameworkCore;

namespace Corely.DataAccess.EntityFramework.UnitOfWork;

internal sealed class EFUnitOfWorkScope
{
    private readonly HashSet<DbContext> _contexts = [];
    public bool IsActive { get; set; }

    public event Action<DbContext>? ContextRegistered;

    public void Register(DbContext context)
    {
        if (context != null)
        {
            if (_contexts.Add(context))
            {
                ContextRegistered?.Invoke(context);
            }
        }
    }

    public IReadOnlyCollection<DbContext> Contexts => _contexts;
}
