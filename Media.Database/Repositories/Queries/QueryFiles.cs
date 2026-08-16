using Cassandra;
using Media.Database.Repositories.Queries.Helpers;
using Npgsql;

#pragma warning disable CS8981 
using cnf = Media.Database.Repositories.Schemas.TablesNoSql.FilesColumns;
using csf = Media.Database.Repositories.Schemas.TablesSql.FilesColumns;
using os = Media.Database.Repositories.Schemas.OrdinalsSql;
using pn = Media.Database.Repositories.Schemas.ParameterNames;
using tn = Media.Database.Repositories.Schemas.TablesNoSql;
using ts = Media.Database.Repositories.Schemas.TablesSql;
#pragma warning restore CS8981 

namespace Media.Database.Repositories.Queries;

internal static class QueryFiles
{
    #region SQL Queries
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

    public static string DeleteSql => $@"
        WITH deleted_rows AS (
            DELETE FROM {ts.Files} 
            WHERE {csf.Id} = {pn.Id}
            RETURNING 1
        )
        SELECT EXISTS(SELECT 1 FROM deleted_rows) AS Any;
        REFRESH MATERIALIZED VIEW CONCURRENTLY {ts.View_Current_Files}
        ;";

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
    public static string GetByIdNoSql => $@"
        SELECT 
            {cnf.Id}, 
            {cnf.SourceMachineId}, 
            {cnf.OriginalFilePath}, 
            {cnf.InsertedOn}, 
            {cnf.UpdatedOn}, 
            {cnf.LastFileUpdate}, 
            {cnf.IsCurrent}, 
            {cnf.Metadata}
        FROM 
            {tn.Files}          
        WHERE 
            {cnf.Id} = {pn.Id} 
        LIMIT 1
        ;";

    public static string InactivateNoSql => $@"
        UPDATE {tn.Files} SET
            {cnf.IsCurrent} = false
        WHERE 
            {cnf.Id} = {pn.Id}
        ;";

    public static string UpsertNoSql => $@"
        INSERT INTO {tn.Files} 
        (
            {cnf.Id}, 
            {cnf.SourceMachineId}, 
            {cnf.OriginalFilePath}, 
            {cnf.InsertedOn}, 
            {cnf.UpdatedOn}, 
            {cnf.LastFileUpdate}, 
            {cnf.IsCurrent}, 
            {cnf.Metadata}
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

    public static string UpdateNoSql => $@"
        UPDATE {tn.Files} SET 
            {cnf.UpdatedOn} = {pn.UpdatedOn},
            {cnf.LastFileUpdate} = {pn.LastFileUpdate},
            {cnf.Metadata} = {pn.Metadata}
        WHERE
            {cnf.Id} = {pn.Id}
        ;";

    public static string DeleteNoSql => $@"
        DELETE FROM {tn.Files} WHERE id = {pn.Id};";
    #endregion

    public static async Task<List<Models.Files>> ToFiles(this NpgsqlDataReader reader)
    {
        List<Models.Files> files = [];

        while (await reader.ReadAsync())
            files.Add(reader.ToFile());

        return files;
    }

    public static Models.Files ToFile(this NpgsqlDataReader reader)
    {
        return new Models.Files
        {
            Id = reader.GetGuid(reader.GetOrdinal(os.Id)),
            SourceMachineId = reader.GetInt32(reader.GetOrdinal(os.SourceMachineId)),
            OriginalFilePath = reader.GetString(reader.GetOrdinal(os.OriginalFilePath)),
            InsertedOn = reader.GetFieldValue<DateTimeOffset>(reader.GetOrdinal(os.InsertedOn)),
            UpdatedOn = reader.GetFieldValue<DateTimeOffset?>(reader.GetOrdinal(os.UpdatedOn)),
            LastFileUpdate = reader.GetFieldValue<DateTimeOffset?>(reader.GetOrdinal(os.LastFileUpdate)),
            IsCurrent = reader.GetFieldValue<bool>(reader.GetOrdinal(os.IsCurrent)),
            Metadata = reader.ToModelOrDefault<Models.Metadata>(os.Metadata)
        };
    }

    public static async Task<List<Guid>> ToIds(this NpgsqlDataReader reader)
    {
        var ids = new List<Guid>();
        while (await reader.ReadAsync())
        {
            ids.Add(reader.GetGuid(reader.GetOrdinal(os.Id)));
        }

        return ids;
    }

    public static Guid ToId(this NpgsqlDataReader reader)
    {
        return reader.GetGuid(reader.GetOrdinal(os.Id));
    }

    public static Models.Files ToFile(this Row row)
    {
        return new Models.Files
        {
            Id = row.GetValue<Guid>(cnf.Id),
            SourceMachineId = row.GetValue<int>(cnf.SourceMachineId),
            OriginalFilePath = row.GetValue<string>(cnf.OriginalFilePath),
            LastFileUpdate = row.GetValue<DateTimeOffset?>(cnf.LastFileUpdate),
            InsertedOn = row.GetValue<DateTimeOffset>(cnf.InsertedOn),
            UpdatedOn = row.GetValue<DateTimeOffset?>(cnf.UpdatedOn),
            IsCurrent = row.GetValue<bool>(cnf.IsCurrent),
            Metadata = row.GetValueOrDefault<Models.Metadata>(cnf.Metadata)
        };
    }
}
