using Npgsql;

#pragma warning disable CS8981 
using csw = Media.Database.Repositories.Schemas.TablesSql.WordsColumns;
using cswf = Media.Database.Repositories.Schemas.TablesSql.WordFilesColumns;
using csvwf = Media.Database.Repositories.Schemas.TablesSql.View_WordFilesColumns;
using os = Media.Database.Repositories.Schemas.OrdinalsSql;
using pn = Media.Database.Repositories.Schemas.ParameterNames;
using ts = Media.Database.Repositories.Schemas.TablesSql;
using Media.Database.Models;
using Microsoft.AspNetCore.Http;
#pragma warning restore CS8981 

namespace Media.Database.Repositories.Queries;

public static class QueryWords
{
    #region SQL Queries
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

    public static string AndFilePages => $@"
            AND ({pn.IsCurrent} IS NULL OR {pn.IsCurrent} = {csvwf.IsCurrent})
            AND ({pn.IsProperName} IS NULL OR {pn.IsProperName} = {csvwf.IsProperName})";
    
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
            {csvwf.IsCurrent} DESC,
            {cswf.Origin}, 
            {csw.Id}, 
            {pn.CameFromFileId}
        FROM 
            inserted_rows
        ON CONFLICT ({cswf.WordId}, {cswf.FileId})
        DO NOTHING;";

    public static string RefreshViewSql => $@"
        REFRESH MATERIALIZED VIEW CONCURRENTLY {ts.View_WordFiles};";

    #endregion

    public static async Task<List<Models.Words>> ToWords(this NpgsqlDataReader reader)
    {
        List<Models.Words> words = [];

        while (await reader.ReadAsync())
            words.Add(reader.ToWord());

        return words;
    }

    public static Models.Words ToWord(this NpgsqlDataReader reader)
    {
        return new Models.Words
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

    public static async Task<List<Models.ViewWordFiles>> ToWordFiles(this NpgsqlDataReader reader)
    {
        List<Models.ViewWordFiles> wordFiles = [];

        while (await reader.ReadAsync())
            wordFiles.Add(reader.ToWordFile());

        return wordFiles;
    }

    public static Models.ViewWordFiles ToWordFile(this NpgsqlDataReader reader)
    {
        return new Models.ViewWordFiles
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
