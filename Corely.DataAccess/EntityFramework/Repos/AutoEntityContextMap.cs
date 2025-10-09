using Corely.DataAccess.Interfaces.Repos;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Concurrent;

namespace Corely.DataAccess.EntityFramework.Repos;

public sealed class AutoEntityContextMap : IEntityContextMap
{
    private static readonly ConcurrentDictionary<Type, Type> _cache = new();
    private readonly IServiceProvider _rootProvider;
    private readonly Type[] _contextTypes;

    public AutoEntityContextMap(IServiceProvider rootProvider, IEnumerable<Type> contextTypes)
    {
        _rootProvider = rootProvider;
        _contextTypes = [.. contextTypes.Distinct()];
    }

    public Type GetContextTypeFor(Type entityType)
        => _cache.GetOrAdd(entityType, ResolveContextType);

    private Type ResolveContextType(Type entityType)
    {
        var matches = new List<Type>();
        foreach (var ctxType in _contextTypes)
        {
            using var scope = _rootProvider.CreateScope();
            var ctx = (DbContext)scope.ServiceProvider.GetRequiredService(ctxType);
            if (ctx.Model.FindEntityType(entityType) != null)
            {
                matches.Add(ctxType);
            }
        }

        return matches.Count switch
        {
            0 => throw new InvalidOperationException($"AutoEntityContextMap: No DbContext model contains entity type {entityType.FullName}"),
            1 => matches[0],
            _ => throw new InvalidOperationException($"AutoEntityContextMap: Ambiguous entity type {entityType.FullName} found in multiple DbContexts: {string.Join(", ", matches.Select(t => t.Name))}. Provide an explicit mapping or segregate models.")
        };
    }
}

