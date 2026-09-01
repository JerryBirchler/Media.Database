using System.Runtime.CompilerServices;

namespace Media.Database.Repositories.Schemas;

/// <summary>
/// Registry of PostgreSQL table and column names, derived from <see cref="Tables"/> and
/// <see cref="ColumnsSql"/> and quoted for case-sensitive identifiers. See <see cref="BaseSchema{TParent, TChild}"/>.
/// </summary>
public class TablesSql : BaseSchema<TablesSql, Tables>
{
    public static readonly string Files = x();
    public static readonly string Registrations = x();
    public static readonly string SourceMachineRegistrations = x();
    public static readonly string View_Current_Files = x();
    public static readonly string View_WordFiles = x();
    public static readonly string Words = x();
    public static readonly string WordFiles = x();

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
        public static readonly string Id = y();
        public static readonly string SourceMachineId = y();
        public static readonly string EmailAddress = y();
        public static readonly string OtpEmail = y();
        public static readonly string CellPhoneNumber = y();
        public static readonly string OtpCellPhone = y();
        public static readonly string IsEmailVerified = y();
        public static readonly string IsSmsVerified = y();
        public static readonly string IsCurrent = y();
        public static readonly string InsertedOn = y();
        public static readonly string UpdatedOn = y();
    }

    public static class SourceMachineRegistrationsColumns
    {
        public static readonly string SourceMachineId = y();
        public static readonly string SourceMachineUuid = y();
        public static readonly string SourceMachineName = y();
        public static readonly string DeviceTypeId = y();
        public static readonly string EmailAddress = y();
        public static readonly string CellPhoneNumber = y();
        public static readonly string FirstName = y();
        public static readonly string LastName = y();
        public static readonly string IsEmailVerified = y();
        public static readonly string IsSmsVerified = y();
        public static readonly string OperatingSystem = y();
        public static readonly string IsActive = y();
        public static readonly string InsertedOn = y();
        public static readonly string UpdatedOn = y();
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

    /// <summary>
    /// Formats a raw table name as a schema-qualified, quoted PostgreSQL identifier.
    /// </summary>
    /// <param name="tableName">The raw table name.</param>
    /// <returns>The formatted identifier, e.g. <c>public."Files"</c>.</returns>
    public static string Format(string tableName)
    {
        return $@"public.""{tableName}""";
    }

    /// <summary>
    /// Resolves the quoted PostgreSQL column identifier for a field declared on a nested
    /// <c>*Columns</c> class, keyed by the field's own name via <see cref="CallerMemberNameAttribute"/>.
    /// Do not pass <paramref name="callerName"/> explicitly.
    /// </summary>
    /// <param name="callerName">The declaring field's name; supplied automatically by the compiler.</param>
    /// <returns>The quoted column identifier, e.g. <c>"Id"</c>.</returns>
#pragma warning disable IDE1006
    public static string y([CallerMemberName] string callerName = "")
#pragma warning restore IDE1006
    {
        var columnName = ColumnsSql.GetField(callerName);

        return $@"""{columnName}""";
    }
}
