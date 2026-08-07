using System.Reflection;
using System.Runtime.CompilerServices;

namespace Media.Database.Repositories.Schemas;

public readonly struct OrdinalsNoSql
{
    public static readonly string Id = x();
    public static readonly string InsertedOn = x();
    public static readonly string IsCurrent = x();
    public static readonly string LastFileUpdate = x();
    public static readonly string Metadata = x();
    public static readonly string OriginalFilePath = x();
    public static readonly string SourceMachineId = x();
    public static readonly string UpdatedOn = x();

#pragma warning disable IDE1006 // Naming Styles
    public static string x([CallerMemberName] string callerName = "")
#pragma warning restore IDE1006 // Naming Styles
    {
        var ordinal = OrdinalsNoSql.GetField(callerName);
        return Ordinals.ToSnake(ordinal);
    }
    public static string GetField(string fieldName)
    {
        FieldInfo field = typeof(OrdinalsNoSql).GetField(fieldName, BindingFlags.Public | BindingFlags.Static)
            ?? throw new ArgumentException($"Ordinal '{fieldName}' was not found in {nameof(OrdinalsNoSql)}.");

        return (string)field.GetValue(null)!;
    }
}
