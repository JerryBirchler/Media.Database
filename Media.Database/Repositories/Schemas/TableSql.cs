using System.Runtime.CompilerServices;

namespace Media.Database.Repositories.Schemas
{
    public readonly struct TableSql
    {
        public static readonly string Files = x();
        public static readonly string Words = x();
        public static readonly string View_Current_Files = x();

#pragma warning disable IDE1006 // Naming Styles
        public static string x([CallerMemberName] string callerName = "")
#pragma warning restore IDE1006 // Naming Styles
        {
            var tableName = Tables.GetTable(callerName);
            return $@"public.""{tableName}""";
        }
    }
}
