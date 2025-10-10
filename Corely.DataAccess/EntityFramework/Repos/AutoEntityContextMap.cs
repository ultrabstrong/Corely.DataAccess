using Corely.DataAccess.Interfaces.Repos;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Concurrent;
using System.Reflection;

namespace Corely.DataAccess.EntityFramework.Repos;

public sealed class AutoEntityContextMap : IEntityContextMap
{
    private static readonly ConcurrentDictionary<Type, Type> _cache = new();
    private readonly IServiceProvider _rootProvider;
    private readonly Lazy<Type[]> _contextTypes;

    public AutoEntityContextMap(IServiceProvider rootProvider)
    {
        _rootProvider = rootProvider;
        _contextTypes = new Lazy<Type[]>(DiscoverRegisteredContextTypes, isThreadSafe: true);
    }

    public Type GetContextTypeFor(Type entityType)
        => _cache.GetOrAdd(entityType, ResolveContextType);

    private Type ResolveContextType(Type entityType)
    {
        var matches = new List<Type>();
        foreach (var ctxType in _contextTypes.Value)
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

    private Type[] DiscoverRegisteredContextTypes()
    {
        var result = new List<Type>();
        var assemblies = AppDomain.CurrentDomain.GetAssemblies();
        foreach (var asm in assemblies)
        {
            Type[] types;
            try
            { types = asm.GetTypes(); }
            catch (ReflectionTypeLoadException ex) { types = [.. ex.Types.Where(t => t != null).Cast<Type>()]; }

            foreach (var t in types)
            {
                if (t == null || t.IsAbstract || !t.IsClass)
                    continue;
                if (!typeof(DbContext).IsAssignableFrom(t))
                    continue;
                using var scope = _rootProvider.CreateScope();
                var resolved = scope.ServiceProvider.GetService(t);
                if (resolved is DbContext)
                {
                    result.Add(t);
                }
            }
        }
        return [.. result.Distinct()];
    }
}

