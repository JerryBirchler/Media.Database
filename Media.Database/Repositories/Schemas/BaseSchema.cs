using Media.Database.Helpers;
using Media.Database.Models;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace Media.Database.Repositories.Schemas;

/// <summary>
/// Base class for the schema field-name registries (<see cref="Ordinals"/>, <see cref="ColumnsSql"/>,
/// <see cref="TablesSql"/>, etc.). Each derived, closed generic type declares a set of
/// <c>public static readonly string</c> fields initialized via <see cref="x"/>, which resolves each
/// field's value from the caller's own member name: verified against the corresponding field on
/// <typeparamref name="TChild"/> (when one is required), then optionally reformatted via a
/// <c>public static string Format(string)</c> method on <typeparamref name="TParent"/>, if present.
/// This lets column/table/parameter name typos be caught at process start via reflection instead
/// of surfacing as runtime SQL errors.
/// </summary>
/// <typeparam name="TParent">The concrete derived schema type declaring the fields.</typeparam>
/// <typeparam name="TChild">
/// The schema type whose field names <typeparamref name="TParent"/>'s fields must match, or
/// <see cref="NoSubFields"/> if no such check/lookup should be performed.
/// </typeparam>
public abstract class BaseSchema<TParent, TChild> : ISchema
    where TParent : BaseSchema<TParent, TChild>, ISchema, new()
    where TChild : class, ISchema, new()
{

    static BaseSchema()
    {
        var key = typeof(BaseSchema<TParent, TChild>);

        if (BaseSchemaCache.Metadata.ContainsKey(key))
            return;

        var metadata = new SchemaMetadata
        {
            ParentType = typeof(TParent)
        };

        if (!BaseSchemaCache.Lookup.TryGetValue(metadata.ParentType, out var parentValue))
        {
            parentValue = new BaseSchemaLookup();
            BaseSchemaCache.Lookup.Add(metadata.ParentType, parentValue);
        }

        metadata.ParentValue = parentValue;
        metadata.ChildType = typeof(TChild);

        MethodInfo? formatMethod = metadata.ParentType.GetMethod("Format", BindingFlags.Public | BindingFlags.Static);

        if (formatMethod is not null)
        {
            metadata.ParentValue.HasFormatter = true;
            metadata.FormatDelegate = (Func<string, string>)Delegate.CreateDelegate(typeof(Func<string, string>), formatMethod);
        }
        else
        {
            metadata.ParentValue.HasFormatter = false;
        }

        if (metadata.ChildType == typeof(NoSubFields))
        {
            metadata.ChildValue = null!;
            metadata.SubFields = null;
            BaseSchemaCache.Metadata[key] = metadata;
            return;
        }

        if (!BaseSchemaCache.Lookup.TryGetValue(metadata.ChildType, out var childValue))
        {
            childValue = new BaseSchemaLookup();
            BaseSchemaCache.Lookup.Add(metadata.ChildType, childValue);
        }

        metadata.ChildValue = childValue;
        metadata.SubFields = new TChild();
        BaseSchemaCache.Metadata[key] = metadata;
    }

    private static SchemaMetadata GetMetadata() => BaseSchemaCache.Metadata[typeof(BaseSchema<TParent, TChild>)];

    /// <summary>
    /// Resolves the value of a field declared on <typeparamref name="TParent"/>, keyed by the
    /// field's own name via <see cref="CallerMemberNameAttribute"/>. Do not pass <paramref name="fieldName"/> explicitly.
    /// </summary>
    /// <param name="fieldName">The declaring field's name; supplied automatically by the compiler.</param>
    /// <returns>The resolved (and, if applicable, formatted) field value.</returns>
#pragma warning disable IDE1006
    public static string x([CallerMemberName] string fieldName = "")
#pragma warning restore IDE1006
    {
        var meta = GetMetadata();

        if (meta.SubFields is null) return fieldName;

        if (meta.ParentValue.Names.TryGetValue(fieldName, out var name))
            return name;

        BaseSchemaCache.GetField(meta.ChildType, fieldName);

        var fieldNameValue = Format(fieldName);

        meta.ParentValue.Names.Add(fieldName, fieldNameValue);
        return fieldNameValue;
    }

    private static string Format(string fieldName)
    {
        var meta = GetMetadata();

        if (meta.ParentValue.HasFormatter == true && meta.FormatDelegate is not null)
            return meta.FormatDelegate(fieldName);

        return fieldName;
    }

    /// <summary>
    /// Looks up the value of a named field declared on <typeparamref name="TParent"/>, via reflection with caching.
    /// </summary>
    /// <param name="fieldName">The name of the field to retrieve.</param>
    /// <returns>The field's string value.</returns>
    /// <exception cref="ArgumentException">Thrown when no such field is declared on <typeparamref name="TParent"/>.</exception>
    public static string GetField(string fieldName)
    {
        var meta = GetMetadata();
        return BaseSchemaCache.GetField(meta.ParentType, fieldName);
    }
}
