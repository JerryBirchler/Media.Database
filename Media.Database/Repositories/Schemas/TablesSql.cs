using System.Runtime.CompilerServices;

namespace Media.Database.Repositories.Schemas;

/// <summary>
/// Registry of PostgreSQL table and column names, derived from <see cref="Tables"/> and
/// <see cref="ColumnsSql"/> and quoted for case-sensitive identifiers. See <see cref="BaseSchema{TParent, TChild}"/>.
/// </summary>
public class TablesSql : BaseSchema<TablesSql, Tables>
{
    /// <summary>Quoted, schema-qualified table name for <c>files</c>.</summary>
    public static readonly string Files = x();

    /// <summary>Quoted, schema-qualified table name for <c>words</c>.</summary>
    public static readonly string Words = x();

    /// <summary>Quoted, schema-qualified table name for <c>word_files</c>.</summary>
    public static readonly string WordFiles = x();

    /// <summary>Quoted, schema-qualified table name for the current-files view.</summary>
    public static readonly string View_Current_Files = x();

    /// <summary>Quoted, schema-qualified table name for the word/files view.</summary>
    public static readonly string View_WordFiles = x();

    /// <summary>
    /// Quoted column names for the <c>files</c> table.
    /// </summary>
    public static class FilesColumns
    {
        /// <summary>Quoted column name for <c>Id</c>.</summary>
        public static readonly string Id = y();

        /// <summary>Quoted column name for <c>SourceMachineId</c>.</summary>
        public static readonly string SourceMachineId = y();

        /// <summary>Quoted column name for <c>OriginalFilePath</c>.</summary>
        public static readonly string OriginalFilePath = y();

        /// <summary>Quoted column name for <c>LastFileUpdate</c>.</summary>
        public static readonly string LastFileUpdate = y();

        /// <summary>Quoted column name for <c>IsCurrent</c>.</summary>
        public static readonly string IsCurrent = y();

        /// <summary>Quoted column name for <c>InsertedOn</c>.</summary>
        public static readonly string InsertedOn = y();

        /// <summary>Quoted column name for <c>UpdatedOn</c>.</summary>
        public static readonly string UpdatedOn = y();

        /// <summary>Quoted column name for <c>Metadata</c>.</summary>
        public static readonly string Metadata = y();
    }

    /// <summary>
    /// Quoted column names for the <c>words</c> table.
    /// </summary>
    public static class WordsColumns
    {
        /// <summary>Quoted column name for <c>Id</c>.</summary>
        public static readonly string Id = y();

        /// <summary>Quoted column name for <c>Word</c>.</summary>
        public static readonly string Word = y();

        /// <summary>Quoted column name for <c>Origin</c>.</summary>
        public static readonly string Origin = y();

        /// <summary>Quoted column name for <c>IsProperName</c>.</summary>
        public static readonly string IsProperName = y();

        /// <summary>Quoted column name for <c>CameFromFileId</c>.</summary>
        public static readonly string CameFromFileId = y();

        /// <summary>Quoted column name for <c>InsertedOn</c>.</summary>
        public static readonly string InsertedOn = y();

        /// <summary>Quoted column name for <c>UpdatedOn</c>.</summary>
        public static readonly string UpdatedOn = y();
    }

    /// <summary>
    /// Quoted column names for the <c>word_files</c> table.
    /// </summary>
    public static class WordFilesColumns
    {
        /// <summary>Quoted column name for <c>Origin</c>.</summary>
        public static readonly string Origin = y();

        /// <summary>Quoted column name for <c>WordId</c>.</summary>
        public static readonly string WordId = y();

        /// <summary>Quoted column name for <c>FileId</c>.</summary>
        public static readonly string FileId = y();
    }

    /// <summary>
    /// Quoted column names for the word/files view.
    /// </summary>
    public static class View_WordFilesColumns
    {
        /// <summary>Quoted column name for <c>Origin</c>.</summary>
        public static readonly string Origin = y();

        /// <summary>Quoted column name for <c>WordId</c>.</summary>
        public static readonly string WordId = y();

        /// <summary>Quoted column name for <c>Word</c>.</summary>
        public static readonly string Word = y();

        /// <summary>Quoted column name for <c>FileId</c>.</summary>
        public static readonly string FileId = y();

        /// <summary>Quoted column name for <c>IsCurrent</c>.</summary>
        public static readonly string IsCurrent = y();

        /// <summary>Quoted column name for <c>IsProperName</c>.</summary>
        public static readonly string IsProperName = y();
    }

    /// <summary>
    /// Formats a raw table name as a schema-qualified, quoted PostgreSQL identifier.
    /// </summary>
    /// <param name="tableName">The raw table name.</param>
    /// <returns>The formatted identifier, e.g. <c>public."Files"</c>.</returns>
    public static string Format(string tableName)
    {
        return $@"public.""{tableName}""";
    }

    /// <summary>
    /// Resolves the quoted PostgreSQL column identifier for a field declared on a nested
    /// <c>*Columns</c> class, keyed by the field's own name via <see cref="CallerMemberNameAttribute"/>.
    /// Do not pass <paramref name="callerName"/> explicitly.
    /// </summary>
    /// <param name="callerName">The declaring field's name; supplied automatically by the compiler.</param>
    /// <returns>The quoted column identifier, e.g. <c>"Id"</c>.</returns>
#pragma warning disable IDE1006
    public static string y([CallerMemberName] string callerName = "")
#pragma warning restore IDE1006
    {
        var columnName = ColumnsSql.GetField(callerName);

        return $@"""{columnName}""";
    }
}
