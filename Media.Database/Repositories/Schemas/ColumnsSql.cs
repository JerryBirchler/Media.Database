namespace Media.Database.Repositories.Schemas;

/// <summary>
/// Registry of the raw (unformatted) field names shared by the PostgreSQL <c>files</c>/<c>words</c>/
/// <c>word_files</c> tables' column-name and ordinal-name schemas. See <see cref="BaseSchema{TParent, TChild}"/>.
/// </summary>
public class ColumnsSql : BaseSchema<ColumnsSql, OrdinalsSql>
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
