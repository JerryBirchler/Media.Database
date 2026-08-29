using Npgsql;

#pragma warning disable CS8981 
using csw = Media.Database.Repositories.Schemas.TablesSql.WordsColumns;
using cswf = Media.Database.Repositories.Schemas.TablesSql.WordFilesColumns;
using csvwf = Media.Database.Repositories.Schemas.TablesSql.View_WordFilesColumns;
using os = Media.Database.Repositories.Schemas.OrdinalsSql;
using pn = Media.Database.Repositories.Schemas.ParameterNames;
using ts = Media.Database.Repositories.Schemas.TablesSql;
using Media.Database.Models;
#pragma warning restore CS8981 

namespace Media.Database.Repositories.Queries;

/// <summary>
/// SQL query text, and reader mapping extensions, for word and word/file records.
/// </summary>
public static class QueryWords
{
    #region SQL Queries
    /// <summary>SQL to select a word by its unique identifier.</summary>
    public static string GetByIdSql => $@"
        SELECT 
            {csw.Id}, 
            {csw.Word}, 
            {csw.Origin},
            {csw.IsProperName},
            {csw.InsertedOn}, 
            {csw.UpdatedOn}, 
            {csw.CameFromFileId}
        FROM 
            {ts.Words}
        WHERE 
            {csw.Id} = {pn.Id} 
        LIMIT 1
        ;";

    /// <summary>Shared SELECT clause for the word/file materialized view, reused by the keyset page queries below.</summary>
    public static string SelectFilePages => $@"
        SELECT 
            {csvwf.Origin}, 
            {csvwf.WordId}, 
            {csvwf.Word}, 
            {csvwf.FileId},
            {csvwf.IsCurrent},
            {csvwf.IsProperName}
        FROM 
            {ts.View_WordFiles}";

    /// <summary>Shared WHERE-clause fragment for filtering the word/file view by current-ness and proper-name status.</summary>
    public static string AndFilePages => $@"
            AND ({pn.IsCurrent} IS NULL OR {pn.IsCurrent} = {csvwf.IsCurrent})
            AND ({pn.IsProperName} IS NULL OR {pn.IsProperName} = {csvwf.IsProperName})";

    /// <summary>SQL to select a keyset-paged page of word/file rows, ordered by word, origin, then file.</summary>
    public static string GetFilePagesByWordOriginSql => $@"
        {SelectFilePages}
        WHERE 
            ({csvwf.Word}, {csvwf.Origin}, {csvwf.FileId}) > 
            (
                COALESCE({pn.Word}, ''), 
                COALESCE({pn.Origin}, -1), 
                COALESCE({pn.FileId}, '00000000-0000-0000-0000-000000000000'::uuid)
            )
            {AndFilePages}
        ORDER BY
            {csvwf.IsCurrent} DESC,
            {csvwf.Word} ASC,
            {csvwf.Origin} ASC,
            {csvwf.FileId} ASC
        LIMIT {pn.Limit}
        ;";

    /// <summary>SQL to select a keyset-paged page of word/file rows, ordered by word, file, then origin.</summary>
    public static string GetFilePagesByWordFileIdSql => $@"
        {SelectFilePages}
        WHERE 
            ({csvwf.Word}, {csvwf.FileId}, {csvwf.Origin}) > 
            (
                COALESCE({pn.Word}, ''), 
                COALESCE({pn.FileId}, '00000000-0000-0000-0000-000000000000'::uuid),
                COALESCE({pn.Origin}, -1) 
            )
            {AndFilePages}
        ORDER BY
            {csvwf.IsCurrent} DESC,
            {csvwf.Word} ASC,
            {csvwf.FileId} ASC,
            {csvwf.Origin} ASC
        LIMIT {pn.Limit}
        ;";

    /// <summary>SQL to select a keyset-paged page of word/file rows, ordered by file, word, then origin.</summary>
    public static string GetFilePagesByFileIdWordSql => $@"
        {SelectFilePages}
        WHERE 
            ({csvwf.FileId}, {csvwf.Word}, {csvwf.Origin}) > 
            (
                COALESCE({pn.FileId}, '00000000-0000-0000-0000-000000000000'::uuid),
                COALESCE({pn.Word}, ''), 
                COALESCE({pn.Origin}, -1) 
            )
            {AndFilePages}
        ORDER BY
            {csvwf.IsCurrent} DESC,
            {csvwf.FileId} ASC,
            {csvwf.Word} ASC,
            {csvwf.Origin} ASC
        LIMIT {pn.Limit}
        ;";

    /// <summary>SQL to select a keyset-paged page of word/file rows, ordered by file, origin, then word.</summary>
    public static string GetFilePagesByFileIdOriginSql => $@"
        {SelectFilePages}
        WHERE 
            ({csvwf.FileId}, {csvwf.Origin}, {csvwf.Word}) > 
            (
                COALESCE({pn.FileId}, '00000000-0000-0000-0000-000000000000'::uuid),
                COALESCE({pn.Origin}, -1),
                COALESCE({pn.Word}, '') 
            )
            {AndFilePages}
        ORDER BY
            {csvwf.IsCurrent} DESC,
            {csvwf.FileId} ASC,
            {csvwf.Origin} ASC,
            {csvwf.Word} ASC
        LIMIT {pn.Limit}
        ;";

    /// <summary>SQL to insert a word (or update it on conflict) and link it to the originating file.</summary>
    public static string UpsertWordSql => $@"
        WITH inserted_rows AS (            
            INSERT INTO {ts.Words} 
            (
                {csw.Word}, 
                {csw.Origin}, 
                {csw.IsProperName},
                {csw.UpdatedOn},
                {csw.CameFromFileId}
            )
            VALUES 
            (
                {pn.Word}, 
                {pn.Origin}, 
                {pn.IsProperName},
                {pn.UpdatedOn},
                {pn.CameFromFileId}
            )
            ON CONFLICT ({csw.Word})
            DO UPDATE SET 
                {csw.IsProperName} = {pn.IsProperName},
                {csw.UpdatedOn} = {pn.UpdatedOn}
            RETURNING *
        )
        INSERT INTO {ts.WordFiles} 
        (
            {cswf.Origin}, 
            {cswf.WordId}, 
            {cswf.FileId} 
        )
        SELECT 
            {cswf.Origin}, 
            {csw.Id}, 
            {pn.CameFromFileId}
        FROM 
            inserted_rows
        ON CONFLICT ({cswf.WordId}, {cswf.FileId})
        DO NOTHING;";

    /// <summary>SQL to refresh the word/file materialized view.</summary>
    public static string RefreshViewSql => $@"
        REFRESH MATERIALIZED VIEW CONCURRENTLY {ts.View_WordFiles};";

    /// <summary>SQL to delete all word/file links for a given file.</summary>
    public static string DeleteFileSql => $@"
        DELETE FROM {ts.WordFiles}
        WHERE {cswf.FileId} = {pn.FileId}";

    #endregion

    /// <summary>Reads every remaining row from <paramref name="reader"/> and maps each to a <see cref="Models.Words"/>.</summary>
    public static async Task<List<Words>> ToWords(this NpgsqlDataReader reader)
    {
        List<Words> words = [];

        while (await reader.ReadAsync())
            words.Add(reader.ToWord());

        return words;
    }

    /// <summary>Maps the current row of <paramref name="reader"/> to a <see cref="Models.Words"/>.</summary>
    public static Words ToWord(this NpgsqlDataReader reader)
    {
        return new Words
        {
            Id = reader.GetInt32(reader.GetOrdinal(os.Id)),
            Word = reader.GetString(reader.GetOrdinal(os.Word)),
            Origin = (WordOrigin)reader.GetInt32(reader.GetOrdinal(os.Origin)),
            IsProperName = reader.GetFieldValue<bool>(reader.GetOrdinal(os.IsProperName)),
            InsertedOn = reader.GetFieldValue<DateTimeOffset>(reader.GetOrdinal(os.InsertedOn)),
            UpdatedOn = reader.GetFieldValue<DateTimeOffset?>(reader.GetOrdinal(os.UpdatedOn)),
            CameFromFileId = reader.GetFieldValue<Guid>(reader.GetOrdinal(os.CameFromFileId)),
        };
    }

    /// <summary>Reads every remaining row from <paramref name="reader"/> and maps each to a <see cref="ViewWordFiles"/>.</summary>
    public static async Task<List<ViewWordFiles>> ToWordFiles(this NpgsqlDataReader reader)
    {
        List<ViewWordFiles> wordFiles = [];

        while (await reader.ReadAsync())
            wordFiles.Add(reader.ToWordFile());

        return wordFiles;
    }

    /// <summary>Maps the current row of <paramref name="reader"/> to a <see cref="ViewWordFiles"/>.</summary>
    public static ViewWordFiles ToWordFile(this NpgsqlDataReader reader)
    {
        return new ViewWordFiles
        {
            Origin = (WordOrigin)reader.GetInt32(reader.GetOrdinal(os.Origin)),
            WordId = reader.GetInt32(reader.GetOrdinal(os.WordId)),
            Word = reader.GetString(reader.GetOrdinal(os.Word)),
            FileId = reader.GetFieldValue<Guid>(reader.GetOrdinal(os.FileId)),
            IsCurrent = reader.GetFieldValue<bool?>(reader.GetOrdinal(os.IsCurrent)),
            IsProperName = reader.GetFieldValue<bool?>(reader.GetOrdinal(os.IsProperName))
        };
    }
}
