namespace Media.Database.Repositories.Schemas;

/// <summary>
/// Canonical registry of table/view names, used unformatted as the root of the table-name
/// validation chain for <see cref="TablesSql"/> and <see cref="TablesCql"/>.
/// See <see cref="BaseSchema{TParent, TChild}"/>.
/// </summary>
public class Tables : BaseSchema<Tables, NoSubFields>
{
    /// <summary>Table name for <c>Files</c>.</summary>
    public static readonly string Files = x();

    /// <summary>Table name for <c>WordFiles</c>.</summary>
    public static readonly string WordFiles = x();

    /// <summary>Table name for <c>Words</c>.</summary>
    public static readonly string Words = x();

    /// <summary>Table name for <c>View_Current_Files</c>.</summary>
    public static readonly string View_Current_Files = x();

    /// <summary>Table name for <c>View_WordFiles</c>.</summary>
    public static readonly string View_WordFiles = x();
}
