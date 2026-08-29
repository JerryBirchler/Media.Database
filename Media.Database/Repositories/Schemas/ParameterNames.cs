namespace Media.Database.Repositories.Schemas;

/// <summary>
/// Registry of PostgreSQL command parameter names (formatted with a leading <c>@</c>), validated
/// against <see cref="Ordinals"/>. See <see cref="BaseSchema{TParent, TChild}"/>.
/// </summary>
public class ParameterNames : BaseSchema<ParameterNames, Ordinals>
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

    /// <summary>
    /// Formats a raw field name as a PostgreSQL command parameter name.
    /// </summary>
    /// <param name="fieldName">The raw field name.</param>
    /// <returns><paramref name="fieldName"/> prefixed with <c>@</c>.</returns>
    public static string Format(string fieldName)
    {
        return "@" + fieldName;
    }
}
