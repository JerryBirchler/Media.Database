using Media.Database.Helpers;
using Npgsql;

#pragma warning disable CS8981
using csw = Media.Database.Repositories.Schemas.TablesSql.WordsColumns;
using cswf = Media.Database.Repositories.Schemas.TablesSql.WordFilesColumns;
using csvwf = Media.Database.Repositories.Schemas.TablesSql.View_WordFilesColumns;
using ccwf = Media.Database.Repositories.Schemas.TablesCql.WordFilesColumns;
using os = Media.Database.Repositories.Schemas.OrdinalsSql;
using pn = Media.Database.Repositories.Schemas.ParameterNames;
using ts = Media.Database.Repositories.Schemas.TablesSql;
using tc = Media.Database.Repositories.Schemas.TablesCql;
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

    /// <summary>Columns shared by every SELECT against the word/file materialized view.</summary>
    private static string SelectFilePagesColumns => $@"
            {csvwf.Origin},
            {csvwf.WordId},
            {csvwf.Word},
            {csvwf.FileId},
            {csvwf.IsCurrent},
            {csvwf.IsProperName},
            {csvwf.OriginalFilePath},
            {csvwf.ThumbnailGeneratedOn}";

    /// <summary>
    /// Shared SELECT clause for the word/file materialized view, plus SourceMachineId -- used by
    /// the fileId/filePath-scoped word lookups, which filter on it (see their own doc comments for
    /// why), and by the word_files Scylla-sync CDC handler's re-query queries.
    /// </summary>
    public static string SelectFilePagesWithSourceMachineId => $@"
        SELECT
            {SelectFilePagesColumns},
            {csvwf.SourceMachineId}
        FROM
            {ts.View_WordFiles}";

    /// <summary>Shared WHERE-clause fragment for filtering the word/file view by current-ness and proper-name status.</summary>
    public static string AndFilePages => $@"
            AND ({pn.IsCurrent} IS NULL OR {pn.IsCurrent} = {csvwf.IsCurrent})
            AND ({pn.IsProperName} IS NULL OR {pn.IsProperName} = {csvwf.IsProperName})";

    /// <summary>
    /// Columns needed to identify a word/file pairing for keyset-pagination cursor computation and
    /// later hydration (see Models.WordFileIdentifier) -- cheap enough to fetch a wide look-ahead
    /// range without paying for a full row nobody is about to render.
    /// </summary>
    private static string SelectIdentifierColumns => $@"
            {csvwf.WordId},
            {csvwf.FileId},
            {csvwf.Word},
            {csvwf.OriginalFilePath}";

    /// <summary>Shared SELECT clause for the identifier-only word/file view queries below.</summary>
    private static string SelectIdentifiers => $@"
        SELECT
            {SelectIdentifierColumns}
        FROM
            {ts.View_WordFiles}";

    /// <summary>Identifier-only page of word/file rows, ordered by word, then origin, then file.</summary>
    public static string GetFileIdentifiersByWordOriginSql => $@"
        {SelectIdentifiers}
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

    /// <summary>Identifier-only page of word/file rows, ordered by word, then file, then origin.</summary>
    public static string GetFileIdentifiersByWordFileIdSql => $@"
        {SelectIdentifiers}
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

    /// <summary>Identifier-only page of word/file rows, ordered by file, then word, then origin.</summary>
    public static string GetFileIdentifiersByFileIdWordSql => $@"
        {SelectIdentifiers}
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

    /// <summary>Identifier-only page of word/file rows, ordered by file, then origin, then word.</summary>
    public static string GetFileIdentifiersByFileIdOriginSql => $@"
        {SelectIdentifiers}
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

    /// <summary>Identifier-only page of word/file rows, ordered by file path, then origin.</summary>
    public static string GetFileIdentifiersByFilePathOriginSql => $@"
        {SelectIdentifiers}
        WHERE
            ({csvwf.OriginalFilePath}, {csvwf.Origin}, {csvwf.Word}, {csvwf.FileId}) >
            (
                COALESCE({pn.OriginalFilePath}, ''),
                COALESCE({pn.Origin}, -1),
                COALESCE({pn.Word}, ''),
                COALESCE({pn.FileId}, '00000000-0000-0000-0000-000000000000'::uuid)
            )
            {AndFilePages}
        ORDER BY
            {csvwf.IsCurrent} DESC,
            {csvwf.OriginalFilePath} ASC,
            {csvwf.Origin} ASC,
            {csvwf.Word} ASC,
            {csvwf.FileId} ASC
        LIMIT {pn.Limit}
        ;";

    /// <summary>Identifier-only page of word/file rows, ordered by file path, then word.</summary>
    public static string GetFileIdentifiersByFilePathWordSql => $@"
        {SelectIdentifiers}
        WHERE
            ({csvwf.OriginalFilePath}, {csvwf.Word}, {csvwf.Origin}, {csvwf.FileId}) >
            (
                COALESCE({pn.OriginalFilePath}, ''),
                COALESCE({pn.Word}, ''),
                COALESCE({pn.Origin}, -1),
                COALESCE({pn.FileId}, '00000000-0000-0000-0000-000000000000'::uuid)
            )
            {AndFilePages}
        ORDER BY
            {csvwf.IsCurrent} DESC,
            {csvwf.OriginalFilePath} ASC,
            {csvwf.Word} ASC,
            {csvwf.Origin} ASC,
            {csvwf.FileId} ASC
        LIMIT {pn.Limit}
        ;";

    /// <summary>Identifier-only page of word/file rows, ordered by word, then file path.</summary>
    public static string GetFileIdentifiersByWordFilePathSql => $@"
        {SelectIdentifiers}
        WHERE
            ({csvwf.Word}, {csvwf.OriginalFilePath}, {csvwf.FileId}, {csvwf.Origin}) >
            (
                COALESCE({pn.Word}, ''),
                COALESCE({pn.OriginalFilePath}, ''),
                COALESCE({pn.FileId}, '00000000-0000-0000-0000-000000000000'::uuid),
                COALESCE({pn.Origin}, -1)
            )
            {AndFilePages}
        ORDER BY
            {csvwf.IsCurrent} DESC,
            {csvwf.Word} ASC,
            {csvwf.OriginalFilePath} ASC,
            {csvwf.FileId} ASC,
            {csvwf.Origin} ASC
        LIMIT {pn.Limit}
        ;";

    /// <summary>
    /// SQL to select a keyset-paged page of a single file's words, ordered by word. FileId is an
    /// equality filter here, not a seek bound -- correct because FileId is already a globally
    /// unique identifier, so no cross-device ambiguity is possible. SourceMachineId is still
    /// required alongside it: it's the one equality this scheme allows, since it comes from the
    /// caller's own X-API-KEY, not client input -- proving the file actually belongs to the
    /// caller before returning anything about it. Uses IX_WordFiles_FileId_Word.
    /// </summary>
    public static string GetWordsByFileIdOrderedByWordSql => $@"
        {SelectFilePagesWithSourceMachineId}
        WHERE
            {csvwf.FileId} = {pn.FileId}
            AND {csvwf.SourceMachineId} = {pn.SourceMachineId}
            AND {csvwf.Word} > COALESCE({pn.Word}, '')
            {AndFilePages}
        ORDER BY
            {csvwf.IsCurrent} DESC,
            {csvwf.Word} ASC
        LIMIT {pn.Limit}
        ;";

    /// <summary>
    /// Same scoping as <see cref="GetWordsByFileIdOrderedByWordSql"/>, ordered by origin instead.
    /// Uses IX_WordFiles_FileId_Origin.
    /// </summary>
    public static string GetWordsByFileIdOrderedByOriginSql => $@"
        {SelectFilePagesWithSourceMachineId}
        WHERE
            {csvwf.FileId} = {pn.FileId}
            AND {csvwf.SourceMachineId} = {pn.SourceMachineId}
            AND {csvwf.Origin} > COALESCE({pn.Origin}, -1)
            {AndFilePages}
        ORDER BY
            {csvwf.IsCurrent} DESC,
            {csvwf.Origin} ASC
        LIMIT {pn.Limit}
        ;";

    /// <summary>
    /// SQL to select a keyset-paged page of a single file path's words, ordered by word.
    /// OriginalFilePath is an equality filter here, not a seek bound -- unlike FileId, a path is
    /// NOT globally unique (different devices can have a file at the same relative path), so
    /// SourceMachineId here is load-bearing for correctness, not just authorization: without it,
    /// this would silently mix another device's same-path file into the results. Uses
    /// IX_WordFiles_FilePath_Word (OriginalFilePath already leads that index, and a real path is
    /// selective enough on its own that a SourceMachineId-leading index isn't needed for this).
    /// </summary>
    public static string GetWordsByFilePathOrderedByWordSql => $@"
        {SelectFilePagesWithSourceMachineId}
        WHERE
            {csvwf.OriginalFilePath} = {pn.OriginalFilePath}
            AND {csvwf.SourceMachineId} = {pn.SourceMachineId}
            AND {csvwf.Word} > COALESCE({pn.Word}, '')
            {AndFilePages}
        ORDER BY
            {csvwf.IsCurrent} DESC,
            {csvwf.Word} ASC
        LIMIT {pn.Limit}
        ;";

    /// <summary>
    /// SQL to select the word/file view row for a single (WordId, FileId) pair, with
    /// SourceMachineId -- used by the word_files Scylla-sync CDC handler when a
    /// "cdc.public.WordFiles" event names exactly one pair to refresh.
    /// </summary>
    public static string GetViewByWordIdAndFileIdSql => $@"
        {SelectFilePagesWithSourceMachineId}
        WHERE
            {csvwf.WordId} = {pn.WordId}
            AND {csvwf.FileId} = {pn.FileId}
        ;";

    /// <summary>
    /// SQL to select every word/file view row for a given word, with SourceMachineId -- used by
    /// the word_files Scylla-sync CDC handler when a "cdc.public.Words" event changes a word that
    /// may be linked to many files.
    /// </summary>
    public static string GetViewByWordIdSql => $@"
        {SelectFilePagesWithSourceMachineId}
        WHERE
            {csvwf.WordId} = {pn.WordId}
        ;";

    /// <summary>
    /// SQL to select every word/file view row for a given file, with SourceMachineId -- used by
    /// the word_files Scylla-sync CDC handler when a "cdc.public.Files" event changes a file that
    /// may be linked to many words.
    /// </summary>
    public static string GetViewByFileIdSql => $@"
        {SelectFilePagesWithSourceMachineId}
        WHERE
            {csvwf.FileId} = {pn.FileId}
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

    /// <summary>
    /// SQL to delete a word by its identifier. Dependent <see cref="Tables.WordFiles"/> rows are
    /// removed automatically at the database level (ON DELETE CASCADE on the WordId foreign key)
    /// — no separate cleanup query is needed. Uploaded files themselves are never touched.
    /// </summary>
    public static string DeleteWordSql => $@"
        DELETE FROM {ts.Words}
        WHERE {csw.Id} = {pn.Id}";

    /// <summary>SQL to select every word currently linked to a file, with each link's own origin.</summary>
    public static string GetWordsByFileIdSql => $@"
        SELECT
            wf.{cswf.WordId},
            w.{csw.Word},
            wf.{cswf.Origin}
        FROM
            {ts.WordFiles} AS wf
        INNER JOIN
            {ts.Words} AS w
        ON
            w.{csw.Id} = wf.{cswf.WordId}
        WHERE
            wf.{cswf.FileId} = {pn.FileId}
        ;";

    /// <summary>
    /// SQL to remove a single word's link to a file (one WordFiles row), without touching the
    /// shared Words row -- other files may still reference the same word.
    /// </summary>
    public static string DeleteWordFileLinkSql => $@"
        DELETE FROM {ts.WordFiles}
        WHERE {cswf.FileId} = {pn.FileId}
          AND {cswf.WordId} = {pn.WordId}
        ;";

    /// <summary>CQL to insert (or overwrite) a word_files row keyed by (WordId, FileId).</summary>
    public static string UpsertWordFilesCql => $@"
        INSERT INTO {tc.WordFiles}
        (
            {ccwf.WordId},
            {ccwf.FileId},
            {ccwf.Origin},
            {ccwf.Word},
            {ccwf.IsCurrent},
            {ccwf.IsProperName},
            {ccwf.OriginalFilePath},
            {ccwf.SourceMachineId},
            {ccwf.ThumbnailGeneratedOn}
        )
        VALUES
        (
            {pn.WordId},
            {pn.FileId},
            {pn.Origin},
            {pn.Word},
            {pn.IsCurrent},
            {pn.IsProperName},
            {pn.OriginalFilePath},
            {pn.SourceMachineId},
            {pn.ThumbnailGeneratedOn}
        )
        ;";

    /// <summary>CQL to delete a word_files row by its (WordId, FileId) key.</summary>
    public static string DeleteWordFilesCql => $@"
        DELETE FROM {tc.WordFiles} WHERE {ccwf.WordId} = {pn.WordId} AND {ccwf.FileId} = {pn.FileId};";

    /// <summary>CQL to select a single word_files row by its (WordId, FileId) key.</summary>
    public static string GetWordFilesByWordIdAndFileIdCql => $@"
        SELECT
            {ccwf.WordId},
            {ccwf.FileId},
            {ccwf.Origin},
            {ccwf.Word},
            {ccwf.IsCurrent},
            {ccwf.IsProperName},
            {ccwf.OriginalFilePath},
            {ccwf.SourceMachineId},
            {ccwf.ThumbnailGeneratedOn}
        FROM
            {tc.WordFiles}
        WHERE
            {ccwf.WordId} = {pn.WordId} AND {ccwf.FileId} = {pn.FileId}
        ;";

    #endregion

    /// <summary>Maps the current row of <paramref name="reader"/> to a <see cref="Models.WordFileIdentifier"/>.</summary>
    public static Models.WordFileIdentifier ToWordFileIdentifier(this NpgsqlDataReader reader)
    {
        return new Models.WordFileIdentifier
        {
            WordId = reader.GetInt32(os.WordId),
            FileId = reader.GetFieldValue<Guid>(os.FileId),
            Word = reader.GetString(os.Word),
            OriginalFilePath = reader.GetString(os.OriginalFilePath)
        };
    }

    /// <summary>Maps a Cassandra/Scylla <paramref name="row"/> to a <see cref="ViewWordFiles"/>.</summary>
    public static ViewWordFiles ToWordFile(this Cassandra.Row row)
    {
        return new ViewWordFiles
        {
            WordId = row.GetValue<int>(ccwf.WordId),
            FileId = row.GetValue<Guid>(ccwf.FileId),
            Origin = (WordOrigin)row.GetValue<int>(ccwf.Origin),
            Word = row.GetValue<string>(ccwf.Word),
            IsCurrent = row.GetValue<bool?>(ccwf.IsCurrent),
            IsProperName = row.GetValue<bool?>(ccwf.IsProperName),
            OriginalFilePath = row.GetValue<string>(ccwf.OriginalFilePath),
            SourceMachineId = row.GetValue<int>(ccwf.SourceMachineId),
            ThumbnailGeneratedOn = row.GetValue<DateTimeOffset?>(ccwf.ThumbnailGeneratedOn)
        };
    }

    /// <summary>Reads every remaining row from <paramref name="reader"/> and maps each to a <see cref="Words"/>.</summary>
    public static async Task<List<Words>> ToWords(this NpgsqlDataReader reader)
    {
        List<Words> words = [];

        while (await reader.ReadAsync())
            words.Add(reader.ToWord());

        return words;
    }

    /// <summary>Maps the current row of <paramref name="reader"/> to a <see cref="Words"/>.</summary>
    public static Words ToWord(this NpgsqlDataReader reader)
    {
        return new Words
        {
            Id = reader.GetInt32(os.Id),
            Word = reader.GetString(os.Word),
            Origin = (WordOrigin)reader.GetInt32(os.Origin),
            IsProperName = reader.GetFieldValue<bool>(os.IsProperName),
            InsertedOn = reader.GetFieldValue<DateTimeOffset>(os.InsertedOn),
            UpdatedOn = reader.GetFieldValue<DateTimeOffset?>(os.UpdatedOn),
            CameFromFileId = reader.GetFieldValue<Guid>(os.CameFromFileId),
        };
    }

    /// <summary>Reads every remaining row from <paramref name="reader"/> and maps each to a <see cref="Models.ViewWordFiles"/>.</summary>
    public static async Task<List<ViewWordFiles>> ToWordFiles(this NpgsqlDataReader reader)
    {
        List<ViewWordFiles> wordFiles = [];

        while (await reader.ReadAsync())
            wordFiles.Add(reader.ToWordFile());

        return wordFiles;
    }

    /// <summary>
    /// Maps the current row of <paramref name="reader"/> to a <see cref="Models.ViewWordFiles"/>,
    /// leaving <see cref="Models.ViewWordFiles.SourceMachineId"/> at its default. Use
    /// <see cref="ToWordFileWithSourceMachineId"/> instead for a row selected via
    /// <see cref="SelectFilePagesWithSourceMachineId"/> that needs it populated.
    /// </summary>
    public static ViewWordFiles ToWordFile(this NpgsqlDataReader reader)
    {
        return new Models.ViewWordFiles
        {
            Origin = (WordOrigin)reader.GetInt32(os.Origin),
            WordId = reader.GetInt32(os.WordId),
            Word = reader.GetString(os.Word),
            FileId = reader.GetFieldValue<Guid>(os.FileId),
            IsCurrent = reader.GetFieldValue<bool?>(os.IsCurrent),
            IsProperName = reader.GetFieldValue<bool?>(os.IsProperName),
            OriginalFilePath = reader.GetString(os.OriginalFilePath),
            ThumbnailGeneratedOn = reader.GetFieldValue<DateTimeOffset?>(os.ThumbnailGeneratedOn)
        };
    }

    /// <summary>
    /// Same as <see cref="ToWordFile"/>, plus SourceMachineId -- for a row selected via
    /// <see cref="SelectFilePagesWithSourceMachineId"/>.
    /// </summary>
    public static ViewWordFiles ToWordFileWithSourceMachineId(this NpgsqlDataReader reader)
    {
        var wordFile = reader.ToWordFile();
        wordFile.SourceMachineId = reader.GetInt32(os.SourceMachineId);
        return wordFile;
    }

    /// <summary>Maps the current row of <paramref name="reader"/> to a word/file link (a word's id and text, and the origin it's linked to a file under).</summary>
    public static WordFileLink ToWordFileLink(this NpgsqlDataReader reader)
    {
        return new WordFileLink(
            reader.GetInt32(os.WordId),
            reader.GetString(os.Word),
            (WordOrigin)reader.GetInt32(os.Origin));
    }
}
