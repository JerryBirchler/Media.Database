using Media.Database.Models;
using System.Reflection;

namespace Media.Database.Helpers;

internal static class BaseSchemaCache
{
    public static readonly Dictionary<Type, BaseSchemaLookup> Lookup = [];
    public static readonly Dictionary<string, FieldInfo> FieldCache = [];
    public static readonly Dictionary<Type, SchemaMetadata> MetadataCache = [];

    public static string GetField(Type derivedType, string fieldName)
    {
        var cacheKey = $"{derivedType.FullName}.{fieldName}";

        if (FieldCache.TryGetValue(cacheKey, out var cachedField))
            return (string)cachedField.GetValue(null)!;

        var name = derivedType.Name;

        FieldInfo field = derivedType.GetField(fieldName, BindingFlags.Public | BindingFlags.Static)
            ?? throw new ArgumentException(string.Format(Constants.NotFound, ToFieldType(name), fieldName, name));

        FieldCache[cacheKey] = field;
        return (string)field.GetValue(null)!;
    }

    private static string ToFieldType(string name)
    {
        ReadOnlySpan<char> span = name;
        ReadOnlySpan<string> suffixes = ["NoSql", "Sql", "s"];

        foreach (var suffix in suffixes)
        {
            ReadOnlySpan<char> suffixSpan = suffix;
            int suffixLength = suffixSpan.Length;

            if (span.Length > suffixLength && span[^suffixLength..].SequenceEqual(suffixSpan))
                span = span[..^suffixLength];
        }

        return span.ToString();
    }
}
