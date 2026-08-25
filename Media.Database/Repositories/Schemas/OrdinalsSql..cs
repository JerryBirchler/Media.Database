namespace Media.Database.Repositories.Schemas;

/// <summary>
/// Registry of field names used as <see cref="Npgsql.NpgsqlDataReader"/> ordinal keys
/// (<c>reader.GetOrdinal(...)</c>) for the PostgreSQL queries, validated against <see cref="Ordinals"/>.
/// See <see cref="BaseSchema{TParent, TChild}"/>.
/// </summary>
public class OrdinalsSql : BaseSchema<OrdinalsSql, Ordinals>
{
    /// <summary>Field name for <c>CameFromFileId</c>.</summary>
    public static readonly string CameFromFileId = x();

    /// <summary>Field name for <c>FileId</c>.</summary>
    public static readonly string FileId = x();

    /// <summary>Field name for <c>Id</c>.</summary>
    public static readonly string Id = x();

    /// <summary>Field name for <c>InsertedOn</c>.</summary>
    public static readonly string InsertedOn = x();

    /// <summary>Field name for <c>IsCurrent</c>.</summary>
    public static readonly string IsCurrent = x();

    /// <summary>Field name for <c>IsProperName</c>.</summary>
    public static readonly string IsProperName = x();

    /// <summary>Field name for <c>LastFileUpdate</c>.</summary>
    public static readonly string LastFileUpdate = x();

    /// <summary>Field name for <c>Metadata</c>.</summary>
    public static readonly string Metadata = x();

    /// <summary>Field name for <c>Origin</c>.</summary>
    public static readonly string Origin = x();

    /// <summary>Field name for <c>OriginalFilePath</c>.</summary>
    public static readonly string OriginalFilePath = x();

    /// <summary>Field name for <c>SourceMachineId</c>.</summary>
    public static readonly string SourceMachineId = x();

    /// <summary>Field name for <c>UpdatedOn</c>.</summary>
    public static readonly string UpdatedOn = x();

    /// <summary>Field name for <c>Word</c>.</summary>
    public static readonly string Word = x();

    /// <summary>Field name for <c>WordId</c>.</summary>
    public static readonly string WordId = x();
}
