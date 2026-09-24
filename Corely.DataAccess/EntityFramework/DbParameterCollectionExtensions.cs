using System.Data.Common;

namespace Corely.DataAccess.EntityFramework;

internal static class DbParameterCollectionExtensions
{
    extension(DbParameterCollection parameters)
    {
        public Dictionary<string, object?> ToLoggingDictionary(bool logValues)
        {
            var dict = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);

            for (var i = 0; i < parameters.Count; i++)
            {
                if (parameters[i] is DbParameter p)
                {
                    var name = string.IsNullOrWhiteSpace(p.ParameterName)
                        ? $"p{i}"
                        : p.ParameterName;
                    var value = logValues ? p.Value : "?";
                    dict[name] = value;
                }
                else
                {
                    dict[$"p{i}"] = logValues ? parameters[i] : "?";
                }
            }

            return dict;
        }
    }
}
