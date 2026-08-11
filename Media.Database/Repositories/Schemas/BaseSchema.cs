using Media.Database.Models;
using Microsoft.AspNetCore.Mvc.Rendering;
using Newtonsoft.Json.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace Media.Database.Repositories.Schemas;

public sealed class NoSubFields { }

public abstract class BaseSchema<T1, T2>
    where T1 : BaseSchema<T1, T2>, new()
    where T2 : class, new()
{
    private static readonly Dictionary<Type, BaseSchemaLookup> _lookup = [];
    private static Type T1Type = null!;
    private static BaseSchemaLookup T1Value = null!;
    private static Type T2Type = null!;
    private static BaseSchemaLookup T2Value = null!;
    private static readonly T2? SubFields = (new Func<T2?>(() =>
    {
        T1Type = typeof(T1);
        _lookup.TryGetValue(T1Type, out T1Value!);

        if (T1Value is null)
        {
            _lookup.Add(T1Type, new());
            T1Value = _lookup[T1Type];
        }

        T2Type = typeof(T2);       
        if (T2Type == typeof(NoSubFields)) return null;
        
        _lookup.TryGetValue(T2Type, out T2Value!);

        if (T2Value is null)
        {
            _lookup.Add(T2Type, new());
            T2Value = _lookup[T2Type];
        }


        return new();
    }))();
#pragma warning disable IDE1006 
    public static string x([CallerMemberName] string fieldName = "")
#pragma warning restore IDE1006 
    {
        if (SubFields is null) return fieldName;
        
        T2Value.Names.TryGetValue(fieldName, out var name);
        if (name is not null)
            return name;

        GetField(T2Type, fieldName);
    
        var fieldNameValue = Format(fieldName);
        T2Value.Names.Add(fieldName, fieldNameValue);
        return fieldNameValue;
    }
    private static string Format(string fieldName)
    {
        if (!T1Value.HasFormatter.HasValue || T1Value.HasFormatter.Value)
        {
            MethodInfo? formatMethod = T1Type.GetMethod("Format", BindingFlags.Public | BindingFlags.Static);

            if (formatMethod is not null)
            {
                T1Value.HasFormatter = true;
                return (string)formatMethod.Invoke(null, [fieldName])!;
            }
            else
                T1Value.HasFormatter = false;
        }

        return fieldName;
    }
   
    public static string GetField(string fieldName)
    {
        return GetField(typeof(T1), fieldName);
    }

    private static string GetField(Type derivedType, string fieldName)
    {
        var name = derivedType.Name;

        FieldInfo field = derivedType.GetField(fieldName, BindingFlags.Public | BindingFlags.Static)
            ?? throw new ArgumentException(string.Format(Constants.NotFound, ToFieldType(name), fieldName, name));

        return (string)field.GetValue(null)!;
    }

    private static string ToFieldType(string name)
    {
        var fieldType = name;

        if (fieldType.EndsWith("NoSql"))
            fieldType = fieldType[..^5];

        else if (fieldType.EndsWith("Sql"))
            fieldType = fieldType[..^3];

        if (fieldType.EndsWith("s"))
            fieldType = fieldType[..^1];

        return fieldType;
    }
}
