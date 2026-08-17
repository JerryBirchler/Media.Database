using Media.Database.Models;
using Media.Database.Helpers;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace Media.Database.Repositories.Schemas;

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

    public static string GetField(string fieldName)
    {
        var meta = GetMetadata();
        return BaseSchemaCache.GetField(meta.ParentType, fieldName);
    }
}
