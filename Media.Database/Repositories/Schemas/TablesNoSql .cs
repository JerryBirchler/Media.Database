using System.Runtime.CompilerServices;
using System.Text;

namespace Media.Database.Repositories.Schemas;

/// <summary>
/// Registry of Scylla/Cassandra table and column names, derived from <see cref="Tables"/> and
/// <see cref="ColumnsNoSql"/> and reformatted to snake_case. See <see cref="BaseSchema{TParent, TChild}"/>.
/// </summary>
public class TablesNoSql : BaseSchema<TablesNoSql, Tables>
{
    /// <summary>CQL table name for the <c>files</c> table.</summary>
    public static readonly string Files = x();

    /// <summary>
    /// Snake_case column names for the <c>files</c> table.
    /// </summary>
    public static class FilesColumns
    {
        /// <summary>Column name for <c>Id</c>.</summary>
        public static readonly string Id = y();

        /// <summary>Column name for <c>SourceMachineId</c>.</summary>
        public static readonly string SourceMachineId = y();

        /// <summary>Column name for <c>OriginalFilePath</c>.</summary>
        public static readonly string OriginalFilePath = y();

        /// <summary>Column name for <c>LastFileUpdate</c>.</summary>
        public static readonly string LastFileUpdate = y();

        /// <summary>Column name for <c>IsCurrent</c>.</summary>
        public static readonly string IsCurrent = y();

        /// <summary>Column name for <c>InsertedOn</c>.</summary>
        public static readonly string InsertedOn = y();

        /// <summary>Column name for <c>UpdatedOn</c>.</summary>
        public static readonly string UpdatedOn = y();

        /// <summary>Column name for <c>Metadata</c>.</summary>
        public static readonly string Metadata = y();
    }

    /// <summary>
    /// Formats a raw table name as its snake_case CQL table name.
    /// </summary>
    /// <param name="tableName">The raw table name.</param>
    /// <returns>The snake_case table name.</returns>
    public static string Format(string tableName)
    {
        return ToSnake(tableName);
    }

    /// <summary>
    /// Resolves the snake_case CQL column name for a field declared on a nested <c>*Columns</c>
    /// class, keyed by the field's own name via <see cref="CallerMemberNameAttribute"/>.
    /// Do not pass <paramref name="callerName"/> explicitly.
    /// </summary>
    /// <param name="callerName">The declaring field's name; supplied automatically by the compiler.</param>
    /// <returns>The snake_case column name.</returns>
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
        StringBuilder sb = new();

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
    #endregion
}
