namespace Media.Database.Repositories.Schemas;

/// <summary>
/// Registry of field names used as Scylla/Cassandra <c>Row</c> column keys for the <c>files</c>
/// table, validated against <see cref="Ordinals"/>. See <see cref="BaseSchema{TParent, TChild}"/>.
/// </summary>
public class OrdinalsCql : BaseSchema<OrdinalsCql, Ordinals>
{
    public static readonly string Id = x();
    public static readonly string InsertedOn = x();
    public static readonly string IsCurrent = x();
    public static readonly string LastFileUpdate = x();
    public static readonly string Metadata = x();
    public static readonly string OriginalFilePath = x();
    public static readonly string SourceMachineId = x();
    public static readonly string UpdatedOn = x();
}
