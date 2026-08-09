using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;

namespace Media.Database.Repositories.Schemas;

public class TableNoSql : BaseSchema<TableNoSql, Tables>
{
    public static readonly string Files = x();

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

    public static string Format(string tableName)
    {
        return ToSnake(tableName);
    }
#pragma warning disable IDE1006
    public static string y([CallerMemberName] string callerName = "")
#pragma warning restore IDE1006 
    {
        var columnName = ColumnsNoSql.GetField(callerName);
        return ToSnake(columnName);
    }

    #region Private methods
    private static string ToSnake(string memberName)
    {   
        StringBuilder sb = new StringBuilder();
        for (int i = 0; i < memberName.Length; i++)
        {
            var value = memberName[i] + "";
            var lower = value.ToLowerInvariant();
            if (lower != value)
            {
                if (i > 0) sb.Append("_");
                sb.Append(lower);
            }
            else
            {
                sb.Append(value);
            }
        }

        return sb.ToString();
    }
    private static string ToLowerFirst(string str)
    {
        if (string.IsNullOrEmpty(str)) return str;

        return char.ToLowerInvariant(str[0]) + str.Substring(1);
    }
    #endregion
}
