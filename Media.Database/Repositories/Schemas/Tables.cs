namespace Media.Database.Repositories.Schemas;

/// <summary>
/// Canonical registry of table/view names, used unformatted as the root of the table-name
/// validation chain for <see cref="TablesSql"/> and <see cref="TablesCql"/>.
/// See <see cref="BaseSchema{TParent, TChild}"/>.
/// </summary>
public class Tables : BaseSchema<Tables, NoSubFields>
{
    public static readonly string CanBeEncryptedFields = x();
    public static readonly string Files = x();
    public static readonly string GroupEncryptionKeys = x();
    public static readonly string Groups = x();
    public static readonly string GroupShell = x();
    public static readonly string GroupsPersons = x();
    public static readonly string GroupsSourceMachines = x();
    public static readonly string GroupUuidOrchestration = x();
    public static readonly string OtpAttempts = x();
    public static readonly string PersonAvatars = x();
    public static readonly string Persons = x();
    public static readonly string PersonsSourceMachines = x();
    public static readonly string PersonVoiceProfiles = x();
    public static readonly string Registrations = x();
    public static readonly string SourceMachineKeys = x();
    public static readonly string SourceMachineRegistrations = x();
    public static readonly string View_Current_Files = x();
    public static readonly string View_WordFiles = x();
    public static readonly string WordFiles = x();
    public static readonly string Words = x();
}
