using Microsoft.EntityFrameworkCore;

namespace Corely.DataAccess.EntityFramework.UnitOfWork;

internal sealed class EFUoWScope
{
    public bool IsActive { get; set; }

    public event Action<DbContext>? ContextRegistered;

    public void Register(DbContext context)
    {
        if (context != null)
        {
            ContextRegistered?.Invoke(context);
        }
    }
}
