using Media.Database.Models;
using System.Reflection;

namespace Media.Database.Helpers;

/// <summary>
/// Internal cache for schema metadata and field information.
/// </summary>
internal static class BaseSchemaCache
{
    /// <summary>
    /// Gets the cache of type-to-BaseSchemaLookup mappings.
    /// </summary>
    public static readonly Dictionary<Type, BaseSchemaLookup> Lookup = [];

    /// <summary>
    /// Gets the cache of field name to FieldInfo mappings.
    /// </summary>
    public static readonly Dictionary<string, FieldInfo> FieldCache = [];

    /// <summary>
    /// Gets the cache of type-to-SchemaMetadata mappings.
    /// </summary>
    public static readonly Dictionary<Type, SchemaMetadata> Metadata = [];

    /// <summary>
    /// Gets the value of a public static field from a derived type using reflection with caching.
    /// </summary>
    /// <param name="derivedType">The type containing the field.</param>
    /// <param name="fieldName">The name of the field to retrieve.</param>
    /// <returns>The string value of the field.</returns>
    /// <exception cref="ArgumentException">Thrown when the field is not found.</exception>
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
