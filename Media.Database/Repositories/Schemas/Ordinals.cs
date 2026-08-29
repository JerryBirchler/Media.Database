namespace Media.Database.Repositories.Schemas;

/// <summary>
/// Canonical registry of field names used across the SQL and NoSQL schemas, used both directly
/// as <c>reader.GetOrdinal(...)</c>/row column keys and as the root of the field-name validation
/// chain for the other schema registries. See <see cref="BaseSchema{TParent, TChild}"/>.
/// </summary>
public class Ordinals : BaseSchema<Ordinals, NoSubFields>
{
    public static readonly string CameFromFileId = x();
    public static readonly string FileId = x();
    public static readonly string Id = x();
    public static readonly string InsertedOn = x();
    public static readonly string IsCurrent = x();
    public static readonly string IsProperName = x();
    public static readonly string LastFileUpdate = x();
    public static readonly string Limit = x();
    public static readonly string Metadata = x();
    public static readonly string Origin = x();
    public static readonly string OriginalFilePath = x();
    public static readonly string SourceMachineId = x();
    public static readonly string UpdatedOn = x();
    public static readonly string Word = x();
    public static readonly string WordId = x();
}
