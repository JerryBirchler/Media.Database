namespace Media.Database.Repositories.Schemas;

internal class ColumnsNoSql : BaseSchema<ColumnsNoSql, OrdinalsSql>
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
