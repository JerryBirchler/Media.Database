using Media.Database.Helpers;
using Media.Database.Models;
using Media.Database.Repositories.Queries.Helpers;
using Npgsql;

#pragma warning disable CS8981
using cgs = Media.Database.Repositories.Schemas.TablesSql.GroupShellColumns;
using csmr = Media.Database.Repositories.Schemas.TablesSql.SourceMachineRegistrationsColumns;
using os = Media.Database.Repositories.Schemas.OrdinalsSql;
using pn = Media.Database.Repositories.Schemas.ParameterNames;
using ts = Media.Database.Repositories.Schemas.TablesSql;
#pragma warning restore CS8981

namespace Media.Database.Repositories.Queries;

/// <summary>
/// SQL query text, and reader mapping extensions, for the GroupShell identity anchor (MEDIA-37) --
/// the lightweight row that always exists the moment a device registers, before any real Group
/// with a Name/Title exists.
/// </summary>
public static class QueryGroupShell
{
    #region SQL Queries

    /// <summary>SQL to insert a new shell.</summary>
    public static string InsertSql => $@"
        INSERT INTO {ts.GroupShell}
        DEFAULT VALUES
        RETURNING
            {cgs.GroupShellId}, {cgs.PromotedGroupId}, {cgs.InsertedOn}, {cgs.UpdatedOn}
        ;";

    /// <summary>SQL to select a shell by its identifier.</summary>
    public static string GetByIdSql => $@"
        SELECT
            {cgs.GroupShellId}, {cgs.PromotedGroupId}, {cgs.InsertedOn}, {cgs.UpdatedOn}
        FROM
            {ts.GroupShell}
        WHERE
            {cgs.GroupShellId} = {pn.Id}
        LIMIT 1
        ;";

    /// <summary>
    /// SQL to permanently set a shell's <c>PromotedGroupId</c> -- a no-op if already promoted, same
    /// "WHERE ... IS NULL" permanence pattern as <c>QueryRegistrations.SetOwningPersonIfUnsetSql</c>.
    /// </summary>
    public static string PromoteIfUnpromotedSql => $@"
        UPDATE {ts.GroupShell} SET
            {cgs.PromotedGroupId} = {pn.GroupId},
            {cgs.UpdatedOn} = {pn.UpdatedOn}
        WHERE
            {cgs.GroupShellId} = {pn.Id}
            AND {cgs.PromotedGroupId} IS NULL
        RETURNING
            {cgs.GroupShellId}, {cgs.PromotedGroupId}, {cgs.InsertedOn}, {cgs.UpdatedOn}
        ;";

    /// <summary>
    /// SQL to list the active devices a person owns whose shell is not yet a group (DATABASE-33),
    /// newest first: what that person can turn into a group of their own.
    /// </summary>
    public static string GetUnpromotedByOwnerSql => $@"
        SELECT
            gs.{cgs.GroupShellId},
            smr.{csmr.SourceMachineId},
            smr.{csmr.SourceMachineName},
            smr.{csmr.DeviceTypeId},
            smr.{csmr.OperatingSystem},
            gs.{cgs.InsertedOn}
        FROM
            {ts.SourceMachineRegistrations} AS smr
        JOIN
            {ts.GroupShell} AS gs
        ON
            gs.{cgs.GroupShellId} = smr.{csmr.GroupShellId}
        WHERE
            smr.{csmr.OwningPersonId} = {pn.OwningPersonId}
            AND smr.{csmr.IsActive} = True
            AND gs.{cgs.PromotedGroupId} IS NULL
        ORDER BY
            gs.{cgs.InsertedOn} DESC
        ;";

    #endregion

    /// <summary>Maps the current row of <paramref name="reader"/> to an <see cref="UnpromotedShell"/>.</summary>
    public static UnpromotedShell ToUnpromotedShell(this NpgsqlDataReader reader)
    {
        return new UnpromotedShell
        {
            GroupShellId = reader.GetInt32(os.GroupShellId),
            SourceMachineId = reader.GetInt32(os.SourceMachineId),
            SourceMachineName = reader.GetString(os.SourceMachineName),
            DeviceTypeId = (DeviceTypes)reader.GetInt32(os.DeviceTypeId),
            OperatingSystem = reader.GetStringOrDefault(os.OperatingSystem),
            InsertedOn = reader.GetFieldValue<DateTimeOffset>(os.InsertedOn)
        };
    }

    /// <summary>Maps the current row of <paramref name="reader"/> to a <see cref="GroupShell"/>.</summary>
    public static GroupShell ToGroupShell(this NpgsqlDataReader reader)
    {
        return new GroupShell
        {
            GroupShellId = reader.GetInt32(os.GroupShellId),
            PromotedGroupId = reader.GetFieldValue<int?>(os.PromotedGroupId),
            InsertedOn = reader.GetFieldValue<DateTimeOffset>(os.InsertedOn),
            UpdatedOn = reader.GetFieldValue<DateTimeOffset?>(os.UpdatedOn)
        };
    }
}
