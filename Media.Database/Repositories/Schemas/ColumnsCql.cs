namespace Media.Database.Repositories.Schemas;

/// <summary>
/// Registry of the raw (unformatted) field names shared by the Scylla/Cassandra <c>files</c>
/// table's column-name and ordinal-name schemas. See <see cref="BaseSchema{TParent, TChild}"/>.
/// </summary>
public class ColumnsCql : BaseSchema<ColumnsCql, OrdinalsCql>
{
    public static readonly string CellPhoneNumber = x();
    public static readonly string DeviceTypeId = x();
    public static readonly string EmailAddress = x();
    public static readonly string FirstName = x();
    public static readonly string Id = x();
    public static readonly string InsertedOn = x();
    public static readonly string IsActive = x();
    public static readonly string IsCurrent = x();
    public static readonly string IsEmailVerified = x();
    public static readonly string IsSmsVerified = x();
    public static readonly string LastFileUpdate = x();
    public static readonly string LastName = x();
    public static readonly string Metadata = x();
    public static readonly string OperatingSystem = x();
    public static readonly string OriginalFilePath = x();
    public static readonly string OtpCellPhone = x();
    public static readonly string OtpEmail = x();
    public static readonly string RegistrationId = x();
    public static readonly string RegistrationInsertedOn = x();
    public static readonly string RegistrationUpdatedOn = x();
    public static readonly string SourceInsertedOn = x();
    public static readonly string SourceMachineId = x();
    public static readonly string SourceMachineName = x();
    public static readonly string SourceMachineUuid = x();
    public static readonly string SourceUpdatedOn = x();
    public static readonly string UpdatedOn = x();
}
