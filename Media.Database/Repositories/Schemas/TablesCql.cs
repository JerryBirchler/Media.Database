using System.Runtime.CompilerServices;
using System.Text;

namespace Media.Database.Repositories.Schemas;

/// <summary>
/// Registry of Scylla/Cassandra table and column names, derived from <see cref="Tables"/> and
/// <see cref="ColumnsCql"/> and reformatted to snake_case. See <see cref="BaseSchema{TParent, TChild}"/>.
/// </summary>
public class TablesCql : BaseSchema<TablesCql, Tables>
{
    public static readonly string CanBeEncryptedFields = x();
    public static readonly string DeviceSearchLists = x();
    public static readonly string Files = x();
    public static readonly string Groups = x();
    public static readonly string GroupSearchLists = x();
    public static readonly string GroupUuidOrchestration = x();
    public static readonly string OtpAttempts = x();
    public static readonly string PersonAvatars = x();
    public static readonly string Persons = x();
    public static readonly string PersonSearchLists = x();
    public static readonly string Registrations = x();
    public static readonly string WordFiles = x();

    public static class CanBeEncryptedFieldsColumns
    {
        public static readonly string TableName = y();
        public static readonly string ColumnName = y();
        public static readonly string ReleaseIntroduced = y();
        public static readonly string ReleaseRemoved = y();
        public static readonly string InsertedOn = y();
        public static readonly string UpdatedOn = y();
    }

    public static class PersonAvatarsColumns
    {
        public static readonly string PersonId = y();
        public static readonly string ContentType = y();
        public static readonly string Image = y();
        public static readonly string UpdatedOn = y();
    }

    public static class OtpAttemptsColumns
    {
        public static readonly string NonceId = y();
        public static readonly string Attempts = y();
        public static readonly string IsUsed = y();
        public static readonly string IssuedOn = y();
    }

    public static class GroupUuidOrchestrationColumns
    {
        public static readonly string PersonUuid = y();
        public static readonly string GroupShellId = y();
        public static readonly string EncryptedUuidBlob = y();
        public static readonly string GroupEncryptionKeyUuid = y();
        public static readonly string UpdatedOn = y();
    }

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

    public static class GroupsColumns
    {
        public static readonly string GroupId = y();
        public static readonly string GroupUuid = y();
        public static readonly string Name = y();
        public static readonly string Title = y();
        public static readonly string Description = y();
        public static readonly string IsActive = y();
        public static readonly string IsEncrypted = y();
        public static readonly string InsertedOn = y();
        public static readonly string UpdatedOn = y();
    }

    public static class PersonsColumns
    {
        public static readonly string PersonId = y();
        public static readonly string PersonUuid = y();
        public static readonly string FirstName = y();
        public static readonly string LastName = y();
        public static readonly string SpokenName = y();
        public static readonly string EmailAddress = y();
        public static readonly string CellPhoneNumber = y();
        public static readonly string IsActive = y();
        public static readonly string CreatedByPersonId = y();
        public static readonly string IsSuperAdmin = y();
        public static readonly string IsEmailVerified = y();
        public static readonly string IsSmsVerified = y();
        public static readonly string OtpWindowOverrideMinutes = y();
        public static readonly string InsertedOn = y();
        public static readonly string UpdatedOn = y();
    }

    public static class WordFilesColumns
    {
        public static readonly string WordId = y();
        public static readonly string WordUuid = y();
        public static readonly string FileId = y();
        public static readonly string Origin = y();
        public static readonly string Word = y();
        public static readonly string IsCurrent = y();
        public static readonly string IsProperName = y();
        public static readonly string WordType = y();
        public static readonly string OriginalFilePath = y();
        public static readonly string SourceMachineId = y();
        public static readonly string ThumbnailGeneratedOn = y();
    }

    public static class RegistrationsColumns
    {
        public static readonly string RegistrationId = y();
        public static readonly string SourceMachineId = y();
        public static readonly string SourceMachineName = y();
        public static readonly string DeviceTypeId = y();
        public static readonly string DisambiguationKey = y();
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
    public static class PersonSearchListsColumns
    {
        public static readonly string PersonId = y();
        public static readonly string PersonSearchListId = y();
        public static readonly string Name = y();
        public static readonly string Payload = y();
        public static readonly string PayloadVersion = y();
    }

    public static class DeviceSearchListsColumns
    {
        public static readonly string SourceMachineId = y();
        public static readonly string DeviceSearchListId = y();
        public static readonly string Name = y();
        public static readonly string Payload = y();
        public static readonly string PayloadVersion = y();
    }

    public static class GroupSearchListsColumns
    {
        public static readonly string GroupId = y();
        public static readonly string GroupSearchListId = y();
        public static readonly string Name = y();
        public static readonly string Payload = y();
        public static readonly string PayloadVersion = y();
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
