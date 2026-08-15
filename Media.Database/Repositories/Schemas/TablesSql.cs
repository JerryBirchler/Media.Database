using System.Runtime.CompilerServices;

namespace Media.Database.Repositories.Schemas;

public class TablesSql : BaseSchema<TablesSql, Tables>
{
    public static readonly string Files = x();
    public static readonly string Words = x();
    public static readonly string WordFiles = x();
    public static readonly string View_Current_Files = x();
    public static readonly string View_WordFiles = x();

    public static class FilesColumns
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
    public static class WordsColumns
    {
        public static readonly string Id = y();
        public static readonly string Word = y();
        public static readonly string Origin = y();
        public static readonly string IsProperName = y();
        public static readonly string CameFromFileId = y();
        public static readonly string InsertedOn = y();
        public static readonly string UpdatedOn = y();
    }
    public static class WordFilesColumns
    {
        public static readonly string Origin = y();
        public static readonly string WordId = y();
        public static readonly string FileId = y();
    }
    public static class View_WordFilesColumns
    {
        public static readonly string Origin = y();
        public static readonly string WordId = y();
        public static readonly string Word = y();
        public static readonly string FileId = y();
        public static readonly string IsCurrent = y();
        public static readonly string IsProperName = y();
    }

    public static string Format(string tableName)
    {
        return $@"public.""{tableName}""";
    }

#pragma warning disable IDE1006 
    public static string y([CallerMemberName] string callerName = "")
#pragma warning restore IDE1006 
    {
        var columnName = ColumnsSql.GetField(callerName);

        return $@"""{columnName}""";
    }
}
