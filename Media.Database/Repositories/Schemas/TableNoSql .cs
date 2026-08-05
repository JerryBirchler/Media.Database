using System.Runtime.CompilerServices;

namespace Media.Database.Repositories.Schemas;

public readonly struct TableNoSql
{
    public static readonly string Files = x();

    public static class TFiles
    {
        public static readonly string Id = y();
        public static readonly string SourceMachineId = y();
        public static readonly string OriginalFilePath = y();
        public static readonly string LastFileUpdate = y();
        public static readonly string IsCurrent = y();
        public static readonly string InsertedOn = y();
        public static readonly string UpdatedOn = y();
        public static readonly string Metadata = y();
    }

#pragma warning disable IDE1006 // Naming Styles
    public static string x([CallerMemberName] string callerName = "")
#pragma warning restore IDE1006 // Naming Styles
    {
        var tableName = Tables.GetTable(callerName);
        return Tables.ToSnake(tableName);
    }
    public static string y([CallerMemberName] string callerName = "")
#pragma warning restore IDE1006 // Naming Styles
    {
        var columnName = ColumnsNoSql.GetField(callerName);
        return Tables.ToSnake(columnName);
    }
}
