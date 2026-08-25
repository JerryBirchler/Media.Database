namespace Media.Database.Repositories.Schemas;

/// <summary>
/// Registry of the raw (unformatted) field names shared by the Scylla/Cassandra <c>files</c>
/// table's column-name and ordinal-name schemas. See <see cref="BaseSchema{TParent, TChild}"/>.
/// </summary>
public class ColumnsNoSql : BaseSchema<ColumnsNoSql, OrdinalsNoSql>
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
