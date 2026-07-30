using Cassandra;
using Media.Database.Repositories.Queries.Helpers;
using Npgsql;

#pragma warning disable CS8981 
using cn = Media.Database.Repositories.Schemas.ColumnsNoSql;
using cs = Media.Database.Repositories.Schemas.ColumnsSql;
using on = Media.Database.Repositories.Schemas.OrdinalsNoSql;
using os = Media.Database.Repositories.Schemas.OrdinalsSql;
using pn = Media.Database.Repositories.Schemas.ParameterNames;
using ts = Media.Database.Repositories.Schemas.TableSql;
#pragma warning restore CS8981 

namespace Media.Database.Repositories.Queries
{
    public static class QueryFiles
    {
        #region SQL Queries
        public static string GetByIdSql => $@"
            SELECT 
                {cs.Id}, 
                {cs.SourceMachineId}, 
                {cs.OriginalFilePath}, 
                {cs.InsertedOn}, 
                {cs.UpdatedOn}, 
                {cs.LastFileUpdate}, 
                {cs.IsCurrent}, 
                {cs.Metadata}
            FROM 
                {ts.Files}
            WHERE 
                {cs.Id} = {pn.Id} 
            LIMIT 1
            ;";

        public static string GetHistoryPagesBySourceMachineIdSql => $@"
            SELECT 
                {cs.Id}, 
                {cs.SourceMachineId}, 
                {cs.OriginalFilePath}, 
                {cs.InsertedOn}, 
                {cs.UpdatedOn}, 
                {cs.LastFileUpdate}, 
                {cs.IsCurrent}, 
                {cs.Metadata}
            FROM 
                {ts.Files}
            WHERE 
                {cs.SourceMachineId} = {pn.SourceMachineId}
                AND {cs.OriginalFilePath} = COALESCE({pn.OriginalFilePath}, '')
            ORDER BY
                {cs.InsertedOn} DESC
            LIMIT @Limit
            ;";

        public static string GetCurrentBySourceMachineIdSql => $@"
            SELECT 
                {cs.Id}, 
                {cs.SourceMachineId}, 
                {cs.OriginalFilePath}, 
                {cs.InsertedOn}, 
                {cs.UpdatedOn}, 
                {cs.LastFileUpdate}, 
                {cs.IsCurrent}, 
                {cs.Metadata}
            FROM 
                {ts.View_Current_Files}
            WHERE 
                {cs.SourceMachineId} = {pn.SourceMachineId}
                AND {cs.OriginalFilePath} = COALESCE({pn.OriginalFilePath}, '')
            LIMIT 1
            ;";

        public static string GetCurrentPagesBySourceMachineIdSql => $@"
            SELECT 
                {cs.Id}, 
                {cs.SourceMachineId}, 
                {cs.OriginalFilePath}, 
                {cs.InsertedOn}, 
                {cs.UpdatedOn}, 
                {cs.LastFileUpdate}, 
                {cs.IsCurrent}, 
                {cs.Metadata}
            FROM 
                {ts.View_Current_Files}
            WHERE 
                {cs.SourceMachineId} = {pn.SourceMachineId}
                AND {cs.OriginalFilePath} > COALESCE({pn.OriginalFilePath}, '')
            ORDER BY
                {cs.SourceMachineId} ASC,
                {cs.OriginalFilePath} ASC
            LIMIT @Limit
            ;";

        public static string GetPreviousIdsSql => $@"
            UPDATE {ts.Files} SET
                {cs.IsCurrent} = false
            WHERE 
                {cs.SourceMachineId} = {pn.SourceMachineId}
                AND {cs.OriginalFilePath} = {pn.OriginalFilePath}
                AND {cs.IsCurrent} = true
            RETURNING 
                {cs.Id}
            ;";

        public static string CreateSql => $@"
            INSERT INTO {ts.Files} 
            (
                {cs.SourceMachineId}, 
                {cs.OriginalFilePath}, 
                {cs.LastFileUpdate}, 
                {cs.Metadata}
            )
            VALUES 
            (
                {pn.SourceMachineId}, 
                {pn.OriginalFilePath}, 
                {pn.LastFileUpdate}, 
                {pn.Metadata}
            )
            RETURNING *;
            REFRESH MATERIALIZED VIEW CONCURRENTLY {ts.View_Current_Files}
            ;";

        public static string UpdateSql => $@"
            UPDATE {ts.Files} SET
                {cs.UpdatedOn} = {pn.UpdatedOn},
                {cs.LastFileUpdate} = {pn.LastFileUpdate},
                {cs.Metadata} = {pn.Metadata}
            WHERE
                {cs.Id} = {pn.Id}
            RETURNING *;
            REFRESH MATERIALIZED VIEW CONCURRENTLY {ts.View_Current_Files}
            ;";

        public static string DeleteSql => $@"
            DELETE FROM {ts.Files} WHERE {cs.Id} = {pn.Id}
            RETURNING *;
            REFRESH MATERIALIZED VIEW CONCURRENTLY {ts.View_Current_Files}
            ;";

        public static string DeleteHistorySql => $@"
            WITH deleted_rows AS (            
                DELETE FROM {ts.Files} 
                WHERE 
                    {cs.SourceMachineId} = {pn.SourceMachineId}
                    AND {cs.OriginalFilePath} = {pn.OriginalFilePath}
                RETURNING *
            )
            SELECT * 
            FROM deleted_rows
            ORDER BY {cs.InsertedOn} DESC;
            REFRESH MATERIALIZED VIEW CONCURRENTLY {ts.View_Current_Files}
            ;";
        #endregion

        #region NoSQL Queries           
        public static string GetByIdNoSql => $@"
            SELECT 
                {cn.Id}, 
                {cn.SourceMachineId}, 
                {cn.OriginalFilePath}, 
                {cn.InsertedOn}, 
                {cn.UpdatedOn}, 
                {cn.LastFileUpdate}, 
                {cn.IsCurrent}, 
                {cn.Metadata}
            FROM 
                files          
            WHERE 
                {cn.Id} = {pn.Id} 
            LIMIT 1
            ;";

        public static string InactivateNoSql => $@"
            UPDATE files SET
                {cn.IsCurrent} = false
            WHERE 
                {cn.Id} = {pn.Id}
            ;";

        public static string CreateNoSql => $@"
            INSERT INTO files 
            (
                {cn.Id}, 
                {cn.SourceMachineId}, 
                {cn.OriginalFilePath}, 
                {cn.InsertedOn}, 
                {cn.LastFileUpdate}, 
                {cn.IsCurrent}, 
                {cn.Metadata}
            )
            VALUES 
            (
                {pn.Id}, 
                {pn.SourceMachineId}, 
                {pn.OriginalFilePath}, 
                {pn.InsertedOn}, 
                {pn.LastFileUpdate}, 
                {pn.IsCurrent}, 
                {pn.Metadata}
            )
            ;";

        public static string UpdateNoSql => $@"
            UPDATE files SET 
                {cn.UpdatedOn} = {pn.UpdatedOn},
                {cn.LastFileUpdate} = {pn.LastFileUpdate},
                {cn.Metadata} = {pn.Metadata}
            WHERE
                {cn.Id} = {pn.Id}
            ;";

        public static string DeleteNoSql => $@"
            DELETE FROM files WHERE id = {pn.Id};";
        #endregion

        public static async Task<List<Models.File>> ToFiles(this NpgsqlDataReader reader)
        {
            List<Models.File> files = [];

            while (await reader.ReadAsync())
                files.Add(reader.ToFile());

            return files;
        }

        public static Models.File ToFile(this NpgsqlDataReader reader)
        {
            return new Models.File
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

        public static Models.File ToFile(this Row row)
        {
            return new Models.File
            {
                Id = row.GetValue<Guid>(on.Id),
                SourceMachineId = row.GetValue<int>(on.SourceMachineId),
                OriginalFilePath = row.GetValue<string>(on.OriginalFilePath),
                LastFileUpdate = row.GetValue<DateTimeOffset?>(on.LastFileUpdate),
                InsertedOn = row.GetValue<DateTimeOffset>(on.InsertedOn),
                UpdatedOn = row.GetValue<DateTimeOffset?>(on.UpdatedOn),
                IsCurrent = row.GetValue<bool>(on.IsCurrent),
                Metadata = row.GetValueOrDefault<Models.Metadata>(on.Metadata)
            };
        }
    }
}
