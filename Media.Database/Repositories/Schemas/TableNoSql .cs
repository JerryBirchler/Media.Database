using System.Runtime.CompilerServices;

namespace Media.Database.Repositories.Schemas;

public readonly struct TableNoSql
{
    public static readonly string Files = x();

#pragma warning disable IDE1006 // Naming Styles
    public static string x([CallerMemberName] string callerName = "")
#pragma warning restore IDE1006 // Naming Styles
    {
        var tableName = Tables.GetTable(callerName);
        return Tables.ToSnake(tableName);
    }
}
