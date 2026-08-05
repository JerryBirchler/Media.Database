using System.Reflection;
using System.Runtime.CompilerServices;

namespace Media.Database.Repositories.Schemas;

public readonly struct ColumnsSql
{
    public static readonly string CameFromFileId = x();
    public static readonly string FileId = x();
    public static readonly string Id = x();
    public static readonly string InsertedOn = x();
    public static readonly string IsCurrent = x();
    public static readonly string IsProperName = x();
    public static readonly string LastFileUpdate = x();
    public static readonly string Metadata = x();
    public static readonly string Origin = x();
    public static readonly string OriginalFilePath = x();
    public static readonly string SourceMachineId = x();
    public static readonly string UpdatedOn = x();
    public static readonly string Word = x();
    public static readonly string WordId = x();

#pragma warning disable IDE1006 // Naming Styles
    public static string x([CallerMemberName] string callerName = "")
#pragma warning restore IDE1006 // Naming Styles
    {
        var ordinal = OrdinalsSql.GetField(callerName);
        ArgumentNullException.ThrowIfNullOrWhiteSpace(ordinal, $"{callerName} not found.");
        return ordinal;
    }
    public static string GetField(string fieldName)
    {
        FieldInfo field = typeof(ColumnsSql).GetField(fieldName, BindingFlags.Public | BindingFlags.Static)
            ?? throw new ArgumentException($"Column '{fieldName}' was not found in {nameof(ColumnsSql)}.");

        return (string)field.GetValue(null)!;
    }

}
