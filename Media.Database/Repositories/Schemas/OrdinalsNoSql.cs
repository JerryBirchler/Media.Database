namespace Media.Database.Repositories.Schemas;

/// <summary>
/// Registry of field names used as Scylla/Cassandra <c>Row</c> column keys for the <c>files</c>
/// table, validated against <see cref="Ordinals"/>. See <see cref="BaseSchema{TParent, TChild}"/>.
/// </summary>
public class OrdinalsNoSql : BaseSchema<OrdinalsNoSql, Ordinals>
{
    /// <summary>Field name for <c>Id</c>.</summary>
    public static readonly string Id = x();

    /// <summary>Field name for <c>InsertedOn</c>.</summary>
    public static readonly string InsertedOn = x();

    /// <summary>Field name for <c>IsCurrent</c>.</summary>
    public static readonly string IsCurrent = x();

    /// <summary>Field name for <c>LastFileUpdate</c>.</summary>
    public static readonly string LastFileUpdate = x();

    /// <summary>Field name for <c>Metadata</c>.</summary>
    public static readonly string Metadata = x();

    /// <summary>Field name for <c>OriginalFilePath</c>.</summary>
    public static readonly string OriginalFilePath = x();

    /// <summary>Field name for <c>SourceMachineId</c>.</summary>
    public static readonly string SourceMachineId = x();

    /// <summary>Field name for <c>UpdatedOn</c>.</summary>
    public static readonly string UpdatedOn = x();
}
