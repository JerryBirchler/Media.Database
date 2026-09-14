using Media.Database.Helpers;
using Media.Database.Mappers;
using Media.Database.Models;
using Npgsql;

#pragma warning disable CS8981
using cgsm = Media.Database.Repositories.Schemas.TablesSql.GroupsSourceMachinesColumns;
using csmr = Media.Database.Repositories.Schemas.TablesSql.SourceMachineRegistrationsColumns;
using os = Media.Database.Repositories.Schemas.OrdinalsSql;
using pn = Media.Database.Repositories.Schemas.ParameterNames;
using ts = Media.Database.Repositories.Schemas.TablesSql;
#pragma warning restore CS8981

namespace Media.Database.Repositories.Queries;

/// <summary>
/// SQL query text, and reader/row mapping extensions, for group/device associations. Same
/// partial-unique-index-driven reactivate-or-insert upsert shape as
/// <see cref="QueryGroupsPersons"/> -- see its remarks.
/// </summary>
public static class QueryGroupsSourceMachines
{
    #region SQL Queries

    /// <summary>
    /// SQL to upsert a group/device association: reactivates any existing row for the pair,
    /// active or not, or inserts a new active row if none exists.
    /// </summary>
    public static string UpsertSql => $@"
        WITH existing AS (
            SELECT {cgsm.GroupSourceMachineId}
            FROM {ts.GroupsSourceMachines}
            WHERE {cgsm.GroupId} = {pn.GroupId} AND {cgsm.SourceMachineId} = {pn.SourceMachineId}
            LIMIT 1
        ),
        updated AS (
            UPDATE {ts.GroupsSourceMachines} SET
                {cgsm.IsActive} = true,
                {cgsm.UpdatedOn} = {pn.UpdatedOn}
            WHERE {cgsm.GroupSourceMachineId} IN (SELECT {cgsm.GroupSourceMachineId} FROM existing)
            RETURNING
                {cgsm.GroupSourceMachineId}, {cgsm.GroupSourceMachineUuid}, {cgsm.GroupId},
                {cgsm.SourceMachineId}, {cgsm.IsActive}, {cgsm.InsertedOn}, {cgsm.UpdatedOn}
        ),
        inserted AS (
            INSERT INTO {ts.GroupsSourceMachines} ({cgsm.GroupId}, {cgsm.SourceMachineId}, {cgsm.IsActive})
            SELECT {pn.GroupId}, {pn.SourceMachineId}, true
            WHERE NOT EXISTS (SELECT 1 FROM existing)
            RETURNING
                {cgsm.GroupSourceMachineId}, {cgsm.GroupSourceMachineUuid}, {cgsm.GroupId},
                {cgsm.SourceMachineId}, {cgsm.IsActive}, {cgsm.InsertedOn}, {cgsm.UpdatedOn}
        )
        SELECT * FROM updated
        UNION ALL
        SELECT * FROM inserted
        ;";

    /// <summary>
    /// SQL to deactivate the active association for a (GroupId, SourceMachineId) pair, returning
    /// the updated row, or no rows if none was active.
    /// </summary>
    public static string DeactivateSql => $@"
        UPDATE {ts.GroupsSourceMachines} SET
            {cgsm.IsActive} = false,
            {cgsm.UpdatedOn} = {pn.UpdatedOn}
        WHERE {cgsm.GroupId} = {pn.GroupId} AND {cgsm.SourceMachineId} = {pn.SourceMachineId} AND {cgsm.IsActive} = true
        RETURNING
            {cgsm.GroupSourceMachineId}, {cgsm.GroupSourceMachineUuid}, {cgsm.GroupId},
            {cgsm.SourceMachineId}, {cgsm.IsActive}, {cgsm.InsertedOn}, {cgsm.UpdatedOn}
        ;";

    /// <summary>
    /// SQL to select just the ordering key (SourceMachineId, SourceMachineName) for a keyset-paged
    /// page of a group's devices, ordered by device name -- cheap enough to identify from
    /// PostgreSQL alone before hydrating full rows from the existing "registrations" Scylla table
    /// (with a PostgreSQL fallback), the same table/CDC pipeline <c>SourceMachineRegistrations</c>
    /// already maintains -- no new Scylla table or CDC handler is needed for this endpoint. The
    /// group/device association (GroupsSourceMachines.IsActive) must always be active -- a device
    /// removed from the group never appears, regardless of the IncludeInactive parameter -- but the
    /// device's own SourceMachineRegistrations.IsActive is only enforced when IncludeInactive is
    /// false, since MEDIA-8 only lets a group admin see inactive devices.
    /// </summary>
    public static string GetSourceMachineIdentifiersByGroupIdSql => $@"
        SELECT
            smr.{csmr.SourceMachineId},
            smr.{csmr.SourceMachineName}
        FROM
            {ts.SourceMachineRegistrations} AS smr
        JOIN
            {ts.GroupsSourceMachines} AS gsm ON gsm.{cgsm.SourceMachineId} = smr.{csmr.SourceMachineId}
        WHERE
            gsm.{cgsm.GroupId} = {pn.GroupId}
            AND gsm.{cgsm.IsActive} = true
            AND ({pn.IncludeInactive} = true OR smr.{csmr.IsActive} = true)
            AND (smr.{csmr.SourceMachineName}, smr.{csmr.SourceMachineId}) >
            (
                COALESCE({pn.SourceMachineName}, ''),
                COALESCE({pn.SourceMachineId}, 0)
            )
        ORDER BY
            smr.{csmr.SourceMachineName} ASC,
            smr.{csmr.SourceMachineId} ASC
        LIMIT {pn.Limit}
        ;";

    #endregion

    /// <summary>
    /// Maps the current row of <paramref name="reader"/> to a <see cref="GroupSourceMachine"/>,
    /// via <paramref name="mapper"/>.
    /// </summary>
    public static GroupSourceMachine ToGroupSourceMachine(this NpgsqlDataReader reader, IMapGroupSourceMachineResponse mapper)
    {
        return mapper.ToGroupSourceMachine(
            reader.GetInt32(os.GroupSourceMachineId),
            reader.GetGuid(os.GroupSourceMachineUuid),
            reader.GetInt32(os.GroupId),
            reader.GetInt32(os.SourceMachineId),
            reader.GetFieldValue<bool>(os.IsActive),
            reader.GetFieldValue<DateTimeOffset>(os.InsertedOn),
            reader.GetFieldValue<DateTimeOffset?>(os.UpdatedOn));
    }

    /// <summary>Maps the current row of <paramref name="reader"/> to its SourceMachineId and SourceMachineName only.</summary>
    public static (int SourceMachineId, string SourceMachineName) ToSourceMachineIdentifier(this NpgsqlDataReader reader)
    {
        return (SourceMachineId: reader.GetInt32(os.SourceMachineId), SourceMachineName: reader.GetString(os.SourceMachineName));
    }

    /// <summary>Reads every remaining row from <paramref name="reader"/> and maps each to its SourceMachineId and SourceMachineName.</summary>
    public static async Task<List<(int SourceMachineId, string SourceMachineName)>> ToSourceMachineIdentifiers(this NpgsqlDataReader reader)
    {
        List<(int SourceMachineId, string SourceMachineName)> identifiers = [];

        while (await reader.ReadAsync())
            identifiers.Add(reader.ToSourceMachineIdentifier());

        return identifiers;
    }
}
