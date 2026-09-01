using Cassandra;
using Media.Database.Helpers;
using Media.Database.Models;
using Media.Database.Repositories.Queries.Helpers;
using Npgsql;

#pragma warning disable CS8981 
using ccf = Media.Database.Repositories.Schemas.TablesCql.FilesColumns;
using csf = Media.Database.Repositories.Schemas.TablesSql.FilesColumns;
using os = Media.Database.Repositories.Schemas.OrdinalsSql;
using pn = Media.Database.Repositories.Schemas.ParameterNames;
using tc = Media.Database.Repositories.Schemas.TablesCql;
using ts = Media.Database.Repositories.Schemas.TablesSql;
#pragma warning restore CS8981 

namespace Media.Database.Repositories.Queries;

/// <summary>
/// SQL and CQL query text, and reader/row mapping extensions, for file records.
/// </summary>
public static class QueryFiles
{
    #region SQL Queries
    /// <summary>SQL to select a file by its unique identifier.</summary>
    public static string GetByIdSql => $@"
        SELECT 
            {csf.Id}, 
            {csf.SourceMachineId}, 
            {csf.OriginalFilePath}, 
            {csf.InsertedOn}, 
            {csf.UpdatedOn}, 
            {csf.LastFileUpdate}, 
            {csf.IsCurrent}, 
            {csf.Metadata}
        FROM 
            {ts.Files}
        WHERE 
            {csf.Id} = {pn.Id} 
        LIMIT 1
        ;";

    /// <summary>SQL to select a page of historical (superseded) files for a source machine and path, newest first.</summary>
    public static string GetHistoryPagesBySourceMachineIdSql => $@"
        SELECT 
            {csf.Id}, 
            {csf.SourceMachineId}, 
            {csf.OriginalFilePath}, 
            {csf.InsertedOn}, 
            {csf.UpdatedOn}, 
            {csf.LastFileUpdate}, 
            {csf.IsCurrent}, 
            {csf.Metadata}
        FROM 
            {ts.Files}
        WHERE 
            {csf.SourceMachineId} = {pn.SourceMachineId}
            AND {csf.OriginalFilePath} = COALESCE({pn.OriginalFilePath}, '')
        ORDER BY
            {csf.InsertedOn} DESC
        LIMIT @Limit
        ;";

    /// <summary>SQL to select the current file for a source machine and path from the current-files view.</summary>
    public static string GetCurrentBySourceMachineIdSql => $@"
        SELECT 
            {csf.Id}, 
            {csf.SourceMachineId}, 
            {csf.OriginalFilePath}, 
            {csf.InsertedOn}, 
            {csf.UpdatedOn}, 
            {csf.LastFileUpdate}, 
            {csf.IsCurrent}, 
            {csf.Metadata}
        FROM 
            {ts.View_Current_Files}
        WHERE 
            {csf.SourceMachineId} = {pn.SourceMachineId}
            AND {csf.OriginalFilePath} = COALESCE({pn.OriginalFilePath}, '')
        LIMIT 1
        ;";

    /// <summary>SQL to select a keyset-paged page of current files, ordered by source machine and path.</summary>
    public static string GetCurrentPagesBySourceMachineIdSql => $@"
        SELECT 
            {csf.Id}, 
            {csf.SourceMachineId}, 
            {csf.OriginalFilePath}, 
            {csf.InsertedOn}, 
            {csf.UpdatedOn}, 
            {csf.LastFileUpdate}, 
            {csf.IsCurrent}, 
            {csf.Metadata}
        FROM 
            {ts.View_Current_Files}
        WHERE 
            ({csf.SourceMachineId}, {csf.OriginalFilePath}) > 
            (
                COALESCE({pn.SourceMachineId}, 0),
                COALESCE({pn.OriginalFilePath}, '')
            )
        ORDER BY
            {csf.SourceMachineId} ASC,
            {csf.OriginalFilePath} ASC
        LIMIT @Limit
        ;";

    /// <summary>SQL to mark all prior current rows for a source machine and path as no longer current, returning their identifiers.</summary>
    public static string GetPreviousIdsSql => $@"
        UPDATE {ts.Files} SET
            {csf.IsCurrent} = false
        WHERE 
            {csf.SourceMachineId} = {pn.SourceMachineId}
            AND {csf.OriginalFilePath} = {pn.OriginalFilePath}
            AND {csf.IsCurrent} = true
        RETURNING 
            {csf.Id}
        ;";

    /// <summary>SQL to insert a new file row (or update it on conflict) and refresh the current-files view.</summary>
    public static string UpsertSql => $@"
        INSERT INTO {ts.Files} 
        (
            {csf.SourceMachineId}, 
            {csf.OriginalFilePath}, 
            {csf.LastFileUpdate}, 
            {csf.Metadata}
        )
        VALUES 
        (
            {pn.SourceMachineId}, 
            {pn.OriginalFilePath}, 
            {pn.LastFileUpdate}, 
            {pn.Metadata}
        )
        ON CONFLICT ({csf.SourceMachineId}, {csf.OriginalFilePath}, {csf.LastFileUpdate})
        DO UPDATE SET 
            {csf.Metadata} = {pn.Metadata},
            {csf.UpdatedOn} = {pn.UpdatedOn}
        RETURNING *;
        REFRESH MATERIALIZED VIEW CONCURRENTLY {ts.View_Current_Files}
        ;";

    /// <summary>SQL to update a file's metadata and timestamps by id, and refresh the current-files view.</summary>
    public static string UpdateSql => $@"
        UPDATE {ts.Files} SET
            {csf.UpdatedOn} = {pn.UpdatedOn},
            {csf.LastFileUpdate} = {pn.LastFileUpdate},
            {csf.Metadata} = {pn.Metadata}
        WHERE
            {csf.Id} = {pn.Id}
        RETURNING *;
        REFRESH MATERIALIZED VIEW CONCURRENTLY {ts.View_Current_Files}
        ;";

    /// <summary>SQL to check whether a file with the given source machine, path, and last-update timestamp already exists.</summary>
    public static string ExistsSql => $@"
        SELECT 
            {csf.Id}
        FROM 
            {ts.Files} 
        WHERE 
            {csf.SourceMachineId} = {pn.SourceMachineId}
            AND {csf.OriginalFilePath} = {pn.OriginalFilePath}
            AND {csf.LastFileUpdate} = {pn.LastFileUpdate}
        LIMIT 1;";

    /// <summary>SQL to delete a file by id and refresh the current-files view.</summary>
    public static string DeleteSql => $@"
        WITH deleted_rows AS (
            DELETE FROM {ts.Files} 
            WHERE {csf.Id} = {pn.Id}
            RETURNING 1
        )
        SELECT EXISTS(SELECT 1 FROM deleted_rows) AS Any;
        REFRESH MATERIALIZED VIEW CONCURRENTLY {ts.View_Current_Files}
        ;";

    /// <summary>SQL to delete all files for a source machine and path, returning the deleted rows, and refresh the current-files view.</summary>
    public static string DeleteHistorySql => $@"
        WITH deleted_rows AS (            
            DELETE FROM {ts.Files} 
            WHERE 
                {csf.SourceMachineId} = {pn.SourceMachineId}
                AND {csf.OriginalFilePath} = {pn.OriginalFilePath}
            RETURNING *
        )
        SELECT * 
        FROM deleted_rows
        ORDER BY {csf.InsertedOn} DESC;
        REFRESH MATERIALIZED VIEW CONCURRENTLY {ts.View_Current_Files}
        ;";
    #endregion

    #region NoSQL Queries
    /// <summary>CQL to select a file by its unique identifier.</summary>
    public static string GetByIdCql => $@"
        SELECT 
            {ccf.Id}, 
            {ccf.SourceMachineId}, 
            {ccf.OriginalFilePath}, 
            {ccf.InsertedOn}, 
            {ccf.UpdatedOn}, 
            {ccf.LastFileUpdate}, 
            {ccf.IsCurrent}, 
            {ccf.Metadata}
        FROM 
            {tc.Files}          
        WHERE 
            {ccf.Id} = {pn.Id} 
        LIMIT 1
        ;";

    /// <summary>CQL to mark a file row as no longer current.</summary>
    public static string InactivateCql => $@"
        UPDATE {tc.Files} SET
            {ccf.IsCurrent} = false
        WHERE 
            {ccf.Id} = {pn.Id}
        ;";

    /// <summary>CQL to insert a file row.</summary>
    public static string UpsertCql => $@"
        INSERT INTO {tc.Files} 
        (
            {ccf.Id}, 
            {ccf.SourceMachineId}, 
            {ccf.OriginalFilePath}, 
            {ccf.InsertedOn}, 
            {ccf.UpdatedOn}, 
            {ccf.LastFileUpdate}, 
            {ccf.IsCurrent}, 
            {ccf.Metadata}
        )
        VALUES 
        (
            {pn.Id}, 
            {pn.SourceMachineId}, 
            {pn.OriginalFilePath}, 
            {pn.InsertedOn}, 
            {pn.UpdatedOn}, 
            {pn.LastFileUpdate}, 
            {pn.IsCurrent}, 
            {pn.Metadata}
        )
        ;";

    /// <summary>CQL to update a file row's metadata and timestamps by id.</summary>
    public static string UpdateCql => $@"
        UPDATE {tc.Files} SET 
            {ccf.UpdatedOn} = {pn.UpdatedOn},
            {ccf.LastFileUpdate} = {pn.LastFileUpdate},
            {ccf.Metadata} = {pn.Metadata}
        WHERE
            {ccf.Id} = {pn.Id}
        ;";

    /// <summary>CQL to delete a file row by id.</summary>
    public static string DeleteCql => $@"
        DELETE FROM {tc.Files} WHERE id = {pn.Id};";
    #endregion

    /// <summary>Reads every remaining row from <paramref name="reader"/> and maps each to a <see cref="Models.Files"/>.</summary>
    public static async Task<List<Files>> ToFiles(this NpgsqlDataReader reader)
    {
        List<Files> files = [];

        while (await reader.ReadAsync())
            files.Add(reader.ToFile());

        return files;
    }

    /// <summary>Maps the current row of <paramref name="reader"/> to a <see cref="Models.Files"/>.</summary>
    public static Files ToFile(this NpgsqlDataReader reader)
    {
        return new Files
        {
            Id = reader.GetGuid(os.Id),
            SourceMachineId = reader.GetInt32(os.SourceMachineId),
            OriginalFilePath = reader.GetString(os.OriginalFilePath),
            InsertedOn = reader.GetFieldValue<DateTimeOffset>(os.InsertedOn),
            UpdatedOn = reader.GetFieldValue<DateTimeOffset?>(os.UpdatedOn),
            LastFileUpdate = reader.GetFieldValue<DateTimeOffset?>(os.LastFileUpdate),
            IsCurrent = reader.GetFieldValue<bool>(os.IsCurrent),
            Metadata = reader.ToModelOrDefault<Models.Metadata>(os.Metadata)
        };
    }

    /// <summary>Reads every remaining row from <paramref name="reader"/> and collects the id column of each.</summary>
    public static async Task<List<Guid>> ToFileIds(this NpgsqlDataReader reader)
    {
        var ids = new List<Guid>();
        while (await reader.ReadAsync())
        {
            ids.Add(reader.GetGuid(os.Id));
        }

        return ids;
    }

    /// <summary>Reads the id column of the current row of <paramref name="reader"/>.</summary>
    public static Guid ToId(this NpgsqlDataReader reader)
    {
        return reader.GetGuid(os.Id);
    }

    /// <summary>Maps a Cassandra/Scylla <paramref name="row"/> to a <see cref="Files"/>.</summary>
    public static Files ToFile(this Row row)
    {
        return new Files
        {
            Id = row.GetValue<Guid>(ccf.Id),
            SourceMachineId = row.GetValue<int>(ccf.SourceMachineId),
            OriginalFilePath = row.GetValue<string>(ccf.OriginalFilePath),
            LastFileUpdate = row.GetValue<DateTimeOffset?>(ccf.LastFileUpdate),
            InsertedOn = row.GetValue<DateTimeOffset>(ccf.InsertedOn),
            UpdatedOn = row.GetValue<DateTimeOffset?>(ccf.UpdatedOn),
            IsCurrent = row.GetValue<bool>(ccf.IsCurrent),
            Metadata = row.GetValueOrDefault<Models.Metadata>(ccf.Metadata)
        };
    }
}
