using System.Reflection;
using System.Runtime.CompilerServices;

namespace Media.Database.Repositories.Schemas
{
    public sealed class NoSubFields { }

    public abstract class BaseSchema<T1, T2>
        where T1 : BaseSchema<T1, T2>, new()
        where T2 : class, new()
    {
        public static readonly T1 Fields = new();
        public static readonly T2? SubFields = typeof(T2) == typeof(NoSubFields) ? null : new();

#pragma warning disable IDE1006 
        public static string x([CallerMemberName] string fieldName = "")
#pragma warning restore IDE1006 
        {
            if (SubFields is null) return fieldName;

            fieldName = GetField(typeof(T2), fieldName);
            MethodInfo? formatMethod = typeof(T1).GetMethod("Format", BindingFlags.Public | BindingFlags.Static);
            
            if (formatMethod is not null)
                fieldName = (string)formatMethod.Invoke(null, [fieldName])!;

            return fieldName;
        }

        public static string GetField(string fieldName)
        {
            return GetField(typeof(T1), fieldName);
        }

        private static string GetField(Type derivedType, string fieldName)
        {
            var name = derivedType.Name;
            var fieldType = name;

            if (fieldType.EndsWith("NoSql"))
                fieldType = fieldType[..^5];

            else if (fieldType.EndsWith("Sql"))
                fieldType = fieldType[..^3];

            if (fieldType.EndsWith("s"))
                fieldType = fieldType[..^1];

            FieldInfo field = derivedType.GetField(fieldName, BindingFlags.Public | BindingFlags.Static)
                ?? throw new ArgumentException(string.Format(Constants.NotFound, fieldType, fieldName, name));

            return (string)field.GetValue(null)!;
        }
    }
}
