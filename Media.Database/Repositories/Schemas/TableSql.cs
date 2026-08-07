using System.Reflection;
using System.Runtime.CompilerServices;

namespace Media.Database.Repositories.Schemas;

public readonly struct TableSql
{
    public static readonly string Files = x();
    public static readonly string Words = x();
    public static readonly string WordFiles = x();
    public static readonly string View_Current_Files = x();
    public static readonly string View_WordFiles = x();

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
    public static class TWords
    {
        public static readonly string Id = y();
        public static readonly string Word = y();
        public static readonly string Origin = y();
        public static readonly string IsProperName = y();
        public static readonly string CameFromFileId = y();
        public static readonly string InsertedOn = y();
        public static readonly string UpdatedOn = y();
    }
    public static class TWordFiles
    {
        public static readonly string Origin = y();
        public static readonly string WordId = y();
        public static readonly string FileId = y();
    }
    public static class TView_WordFiles
    {
        public static readonly string Origin = y();
        public static readonly string WordId = y();
        public static readonly string Word = y();
        public static readonly string FileId = y();
    }
#pragma warning disable IDE1006 // Naming Styles
    public static string x([CallerMemberName] string callerName = "")
#pragma warning restore IDE1006 // Naming Styles
    {
        var tableName = Tables.GetTable(callerName);
        ArgumentException.ThrowIfNullOrEmpty($"{callerName} is not found");
        return $@"public.""{tableName}""";
    }
#pragma warning disable IDE1006 // Naming Styles
    public static string y([CallerMemberName] string callerName = "")
#pragma warning restore IDE1006 // Naming Styles
    {
        var columnName = ColumnsSql.GetField(callerName);
        ArgumentException.ThrowIfNullOrEmpty($"{callerName} is not found");
        return $@"""{columnName}""";
    }
}
