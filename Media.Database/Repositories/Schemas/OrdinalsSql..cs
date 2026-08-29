namespace Media.Database.Repositories.Schemas;

/// <summary>
/// Registry of field names used as <see cref="Npgsql.NpgsqlDataReader"/> ordinal keys
/// (<c>reader.GetOrdinal(...)</c>) for the PostgreSQL queries, validated against <see cref="Ordinals"/>.
/// See <see cref="BaseSchema{TParent, TChild}"/>.
/// </summary>
public class OrdinalsSql : BaseSchema<OrdinalsSql, Ordinals>
{
    public static readonly string CameFromFileId = x();
    public static readonly string FileId = x();
    public static readonly string Id = x();
    public static readonly string InsertedOn = x();
    public static readonly string IsCurrent = x();
    public static readonly string IsProperName = x();
    public static readonly string LastFileUpdate = x();
    public static readonly string Metadata = x();
    public static readonly string Origin = x();
    public static readonly string OriginalFilePath = x();
    public static readonly string SourceMachineId = x();
    public static readonly string UpdatedOn = x();
    public static readonly string Word = x();
    public static readonly string WordId = x();
}
