using Media.Database.Helpers;
using Media.Database.Models;
using Npgsql;

#pragma warning disable CS8981
using cgs = Media.Database.Repositories.Schemas.TablesSql.GroupShellColumns;
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

    #endregion

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
