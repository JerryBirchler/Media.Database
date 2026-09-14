using Media.Database.Helpers;
using Media.Database.Mappers;
using Media.Database.Models;
using Npgsql;

#pragma warning disable CS8981
using cg = Media.Database.Repositories.Schemas.TablesSql.GroupsColumns;
using os = Media.Database.Repositories.Schemas.OrdinalsSql;
using pn = Media.Database.Repositories.Schemas.ParameterNames;
using ts = Media.Database.Repositories.Schemas.TablesSql;
#pragma warning restore CS8981

namespace Media.Database.Repositories.Queries;

/// <summary>
/// SQL query text, and reader/row mapping extensions, for groups.
/// </summary>
public static class QueryGroups
{
    #region SQL Queries

    /// <summary>SQL to insert a new group, returning the inserted row.</summary>
    public static string AddGroupSql => $@"
        INSERT INTO {ts.Groups} (
            {cg.Name},
            {cg.Title},
            {cg.Description},
            {cg.IsActive}
        ) VALUES (
            {pn.Name},
            {pn.Title},
            {pn.Description},
            {pn.IsActive}
        )
        RETURNING
            {cg.GroupId},
            {cg.GroupUuid},
            {cg.Name},
            {cg.Title},
            {cg.Description},
            {cg.IsActive},
            {cg.InsertedOn},
            {cg.UpdatedOn}
        ;";

    /// <summary>SQL to select a group by its <c>GroupUuid</c>.</summary>
    public static string GetByUuidSql => $@"
        SELECT
            {cg.GroupId},
            {cg.GroupUuid},
            {cg.Name},
            {cg.Title},
            {cg.Description},
            {cg.IsActive},
            {cg.InsertedOn},
            {cg.UpdatedOn}
        FROM {ts.Groups}
        WHERE {cg.GroupUuid} = {pn.GroupUuid}
        ;";

    /// <summary>SQL to select a group by its unique, case-insensitive <c>Name</c>.</summary>
    public static string GetByNameSql => $@"
        SELECT
            {cg.GroupId},
            {cg.GroupUuid},
            {cg.Name},
            {cg.Title},
            {cg.Description},
            {cg.IsActive},
            {cg.InsertedOn},
            {cg.UpdatedOn}
        FROM {ts.Groups}
        WHERE {cg.Name} = {pn.Name}
        ;";

    /// <summary>
    /// SQL to partially update a group's <c>Title</c>/<c>Description</c> (whichever is
    /// non-null/provided is applied via <c>COALESCE</c> against the existing value), returning
    /// the updated row.
    /// </summary>
    public static string UpdateGroupSql => $@"
        UPDATE {ts.Groups} SET
            {cg.Title} = COALESCE({pn.Title}, {cg.Title}),
            {cg.Description} = COALESCE({pn.Description}, {cg.Description}),
            {cg.UpdatedOn} = {pn.UpdatedOn}
        WHERE {cg.GroupId} = {pn.GroupId}
        RETURNING
            {cg.GroupId},
            {cg.GroupUuid},
            {cg.Name},
            {cg.Title},
            {cg.Description},
            {cg.IsActive},
            {cg.InsertedOn},
            {cg.UpdatedOn}
        ;";

    /// <summary>SQL to set a group's <c>IsActive</c> flag, returning the updated row.</summary>
    public static string SetActiveSql => $@"
        UPDATE {ts.Groups} SET
            {cg.IsActive} = {pn.IsActive},
            {cg.UpdatedOn} = {pn.UpdatedOn}
        WHERE {cg.GroupId} = {pn.GroupId}
        RETURNING
            {cg.GroupId},
            {cg.GroupUuid},
            {cg.Name},
            {cg.Title},
            {cg.Description},
            {cg.IsActive},
            {cg.InsertedOn},
            {cg.UpdatedOn}
        ;";

    #endregion

    /// <summary>
    /// Maps the current row of <paramref name="reader"/> to a <see cref="Group"/>, via
    /// <paramref name="mapper"/>.
    /// </summary>
    public static Group ToGroup(this NpgsqlDataReader reader, IMapGroupResponse mapper)
    {
        return mapper.ToGroup(
            reader.GetInt32(os.GroupId),
            reader.GetGuid(os.GroupUuid),
            reader.GetString(os.Name),
            reader.GetString(os.Title),
            reader.GetFieldValue<string?>(os.Description),
            reader.GetFieldValue<bool>(os.IsActive),
            reader.GetFieldValue<DateTimeOffset>(os.InsertedOn),
            reader.GetFieldValue<DateTimeOffset?>(os.UpdatedOn));
    }
}
