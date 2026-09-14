using Media.Database.Helpers;
using Media.Database.Mappers;
using Media.Database.Models;
using Npgsql;

#pragma warning disable CS8981
using cpsm = Media.Database.Repositories.Schemas.TablesSql.PersonsSourceMachinesColumns;
using os = Media.Database.Repositories.Schemas.OrdinalsSql;
using pn = Media.Database.Repositories.Schemas.ParameterNames;
using ts = Media.Database.Repositories.Schemas.TablesSql;
#pragma warning restore CS8981

namespace Media.Database.Repositories.Queries;

/// <summary>
/// SQL query text, and reader/row mapping extensions, for person/device associations. Always a
/// plain, passive association -- see <see cref="PersonSourceMachine"/>.
/// </summary>
public static class QueryPersonsSourceMachines
{
    #region SQL Queries

    /// <summary>
    /// SQL to select the active association for a (PersonId, SourceMachineId) pair, or no rows if
    /// none is active.
    /// </summary>
    public static string GetActiveSql => $@"
        SELECT
            {cpsm.PersonSourceMachineId}, {cpsm.PersonSourceMachineUuid}, {cpsm.PersonId},
            {cpsm.SourceMachineId}, {cpsm.IsActive}, {cpsm.InsertedOn}, {cpsm.UpdatedOn}
        FROM {ts.PersonsSourceMachines}
        WHERE {cpsm.PersonId} = {pn.PersonId} AND {cpsm.SourceMachineId} = {pn.SourceMachineId} AND {cpsm.IsActive} = true
        ;";

    /// <summary>SQL to select every active device association for a person.</summary>
    public static string ListActiveByPersonSql => $@"
        SELECT
            {cpsm.PersonSourceMachineId}, {cpsm.PersonSourceMachineUuid}, {cpsm.PersonId},
            {cpsm.SourceMachineId}, {cpsm.IsActive}, {cpsm.InsertedOn}, {cpsm.UpdatedOn}
        FROM {ts.PersonsSourceMachines}
        WHERE {cpsm.PersonId} = {pn.PersonId} AND {cpsm.IsActive} = true
        ;";

    /// <summary>SQL to insert a new active person/device association, returning the inserted row.</summary>
    public static string AddSql => $@"
        INSERT INTO {ts.PersonsSourceMachines} ({cpsm.PersonId}, {cpsm.SourceMachineId}, {cpsm.IsActive})
        VALUES ({pn.PersonId}, {pn.SourceMachineId}, true)
        RETURNING
            {cpsm.PersonSourceMachineId}, {cpsm.PersonSourceMachineUuid}, {cpsm.PersonId},
            {cpsm.SourceMachineId}, {cpsm.IsActive}, {cpsm.InsertedOn}, {cpsm.UpdatedOn}
        ;";

    /// <summary>
    /// SQL to upsert a person/device association: reactivates any existing row for the pair
    /// (active or not), or inserts a new active row if none exists -- the identical
    /// reactivate-or-insert pattern <see cref="QueryGroupsPersons.UpsertSql"/> uses, minus the
    /// <c>IsAdmin</c> column this table has no equivalent of (see <see cref="PersonSourceMachine"/>).
    /// </summary>
    public static string UpsertSql => $@"
        WITH existing AS (
            SELECT {cpsm.PersonSourceMachineId}
            FROM {ts.PersonsSourceMachines}
            WHERE {cpsm.PersonId} = {pn.PersonId} AND {cpsm.SourceMachineId} = {pn.SourceMachineId}
            LIMIT 1
        ),
        updated AS (
            UPDATE {ts.PersonsSourceMachines} SET
                {cpsm.IsActive} = true,
                {cpsm.UpdatedOn} = {pn.UpdatedOn}
            WHERE {cpsm.PersonSourceMachineId} IN (SELECT {cpsm.PersonSourceMachineId} FROM existing)
            RETURNING
                {cpsm.PersonSourceMachineId}, {cpsm.PersonSourceMachineUuid}, {cpsm.PersonId},
                {cpsm.SourceMachineId}, {cpsm.IsActive}, {cpsm.InsertedOn}, {cpsm.UpdatedOn}
        ),
        inserted AS (
            INSERT INTO {ts.PersonsSourceMachines} ({cpsm.PersonId}, {cpsm.SourceMachineId}, {cpsm.IsActive})
            SELECT {pn.PersonId}, {pn.SourceMachineId}, true
            WHERE NOT EXISTS (SELECT 1 FROM existing)
            RETURNING
                {cpsm.PersonSourceMachineId}, {cpsm.PersonSourceMachineUuid}, {cpsm.PersonId},
                {cpsm.SourceMachineId}, {cpsm.IsActive}, {cpsm.InsertedOn}, {cpsm.UpdatedOn}
        )
        SELECT * FROM updated
        UNION ALL
        SELECT * FROM inserted
        ;";

    #endregion

    /// <summary>
    /// Maps the current row of <paramref name="reader"/> to a <see cref="PersonSourceMachine"/>,
    /// via <paramref name="mapper"/>.
    /// </summary>
    public static PersonSourceMachine ToPersonSourceMachine(this NpgsqlDataReader reader, IMapPersonSourceMachineResponse mapper)
    {
        return mapper.ToPersonSourceMachine(
            reader.GetInt32(os.PersonSourceMachineId),
            reader.GetGuid(os.PersonSourceMachineUuid),
            reader.GetInt32(os.PersonId),
            reader.GetInt32(os.SourceMachineId),
            reader.GetFieldValue<bool>(os.IsActive),
            reader.GetFieldValue<DateTimeOffset>(os.InsertedOn),
            reader.GetFieldValue<DateTimeOffset?>(os.UpdatedOn));
    }
}
