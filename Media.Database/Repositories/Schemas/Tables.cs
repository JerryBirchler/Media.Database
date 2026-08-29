namespace Media.Database.Repositories.Schemas;

/// <summary>
/// Canonical registry of table/view names, used unformatted as the root of the table-name
/// validation chain for <see cref="TablesSql"/> and <see cref="TablesCql"/>.
/// See <see cref="BaseSchema{TParent, TChild}"/>.
/// </summary>
public class Tables : BaseSchema<Tables, NoSubFields>
{
    public static readonly string Files = x();
    public static readonly string WordFiles = x();
    public static readonly string Words = x();
    public static readonly string View_Current_Files = x();
    public static readonly string View_WordFiles = x();
}
