using Media.Database.Models;
using Media.Database.Helpers;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace Media.Database.Repositories.Schemas;

internal abstract class BaseSchema<T1, T2> : ISchema
    where T1 : BaseSchema<T1, T2>, ISchema, new()
    where T2 : class, ISchema, new()
{

    static BaseSchema()
    {
        var key = typeof(BaseSchema<T1, T2>);
        var metadata = new SchemaMetadata
        {
            T1Type = typeof(T1)
        };

        if (!BaseSchemaCache.Lookup.TryGetValue(metadata.T1Type, out var t1Value))
        {
            t1Value = new BaseSchemaLookup();
            BaseSchemaCache.Lookup.Add(metadata.T1Type, t1Value);
        }

        metadata.T1Value = t1Value;
        metadata.T2Type = typeof(T2);

        if (metadata.T2Type == typeof(NoSubFields))
        {
            metadata.T2Value = null!;
            metadata.SubFields = null;
            BaseSchemaCache.MetadataCache[key] = metadata;
            return;
        }

        if (!BaseSchemaCache.Lookup.TryGetValue(metadata.T2Type, out var t2Value))
        {
            t2Value = new BaseSchemaLookup();
            BaseSchemaCache.Lookup.Add(metadata.T2Type, t2Value);
        }

        metadata.T2Value = t2Value;
        MethodInfo? formatMethod = metadata.T1Type.GetMethod("Format", BindingFlags.Public | BindingFlags.Static);

        if (formatMethod is not null)
        {
            metadata.T1Value.HasFormatter = true;
            metadata.FormatDelegate = (Func<string, string>)Delegate.CreateDelegate(typeof(Func<string, string>), formatMethod);
        }
        else
        {
            metadata.T1Value.HasFormatter = false;
        }

        metadata.SubFields = new T2();
        BaseSchemaCache.MetadataCache[key] = metadata;
    }

    private static SchemaMetadata GetMetadata() => BaseSchemaCache.MetadataCache[typeof(BaseSchema<T1, T2>)];

#pragma warning disable IDE1006 
    public static string x([CallerMemberName] string fieldName = "")
#pragma warning restore IDE1006 
    {
        var meta = GetMetadata();
 
        if (meta.SubFields is null) return fieldName;

        if (meta.T2Value.Names.TryGetValue(fieldName, out var name))
            return name;

        BaseSchemaCache.GetField(meta.T2Type, fieldName);

        var fieldNameValue = Format(fieldName);

        meta.T2Value.Names.Add(fieldName, fieldNameValue);
        return fieldNameValue;
    }

    private static string Format(string fieldName)
    {
        var meta = GetMetadata();

        if (meta.T1Value.HasFormatter == true && meta.FormatDelegate is not null)
            return meta.FormatDelegate(fieldName);

        return fieldName;
    }

    public static string GetField(string fieldName)
    {
        var meta = GetMetadata();
        return BaseSchemaCache.GetField(meta.T1Type, fieldName);
    }
}
