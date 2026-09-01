using System.Runtime.CompilerServices;
using System.Text;

namespace Media.Database.Repositories.Schemas;

/// <summary>
/// Registry of Scylla/Cassandra table and column names, derived from <see cref="Tables"/> and
/// <see cref="ColumnsCql"/> and reformatted to snake_case. See <see cref="BaseSchema{TParent, TChild}"/>.
/// </summary>
public class TablesCql : BaseSchema<TablesCql, Tables>
{
    public static readonly string Files = x();
    public static readonly string Registrations = x();

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

    public static class RegistrationsColumns
    {
        public static readonly string SourceMachineUuid = y();
        public static readonly string RegistrationId = y();
        public static readonly string SourceMachineId = y();
        public static readonly string SourceMachineName = y();
        public static readonly string DeviceTypeId = y();
        public static readonly string FirstName = y();
        public static readonly string LastName = y();
        public static readonly string EmailAddress = y();
        public static readonly string CellPhoneNumber = y();
        public static readonly string OperatingSystem = y();
        public static readonly string SourceInsertedOn = y();
        public static readonly string SourceUpdatedOn = y();
        public static readonly string IsActive = y();
        public static readonly string IsEmailVerified = y();
        public static readonly string IsSmsVerified = y();
        public static readonly string OtpEmail = y();
        public static readonly string OtpCellPhone = y();
        public static readonly string RegistrationInsertedOn = y();
        public static readonly string RegistrationUpdatedOn = y();
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
        var columnName = ColumnsCql.GetField(callerName);
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
