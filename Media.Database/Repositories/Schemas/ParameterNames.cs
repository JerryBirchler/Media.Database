namespace Media.Database.Repositories.Schemas;

/// <summary>
/// Registry of PostgreSQL command parameter names (formatted with a leading <c>@</c>), validated
/// against <see cref="Ordinals"/>. See <see cref="BaseSchema{TParent, TChild}"/>.
/// </summary>
public class ParameterNames : BaseSchema<ParameterNames, Ordinals>
{
    /// <summary>Parameter name for <c>CameFromFileId</c>.</summary>
    public static readonly string CameFromFileId = x();

    /// <summary>Parameter name for <c>FileId</c>.</summary>
    public static readonly string FileId = x();

    /// <summary>Parameter name for <c>Id</c>.</summary>
    public static readonly string Id = x();

    /// <summary>Parameter name for <c>InsertedOn</c>.</summary>
    public static readonly string InsertedOn = x();

    /// <summary>Parameter name for <c>IsCurrent</c>.</summary>
    public static readonly string IsCurrent = x();

    /// <summary>Parameter name for <c>IsProperName</c>.</summary>
    public static readonly string IsProperName = x();

    /// <summary>Parameter name for <c>LastFileUpdate</c>.</summary>
    public static readonly string LastFileUpdate = x();

    /// <summary>Parameter name for <c>Limit</c>.</summary>
    public static readonly string Limit = x();

    /// <summary>Parameter name for <c>Metadata</c>.</summary>
    public static readonly string Metadata = x();

    /// <summary>Parameter name for <c>Origin</c>.</summary>
    public static readonly string Origin = x();

    /// <summary>Parameter name for <c>OriginalFilePath</c>.</summary>
    public static readonly string OriginalFilePath = x();

    /// <summary>Parameter name for <c>SourceMachineId</c>.</summary>
    public static readonly string SourceMachineId = x();

    /// <summary>Parameter name for <c>UpdatedOn</c>.</summary>
    public static readonly string UpdatedOn = x();

    /// <summary>Parameter name for <c>Word</c>.</summary>
    public static readonly string Word = x();

    /// <summary>Parameter name for <c>WordId</c>.</summary>
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
