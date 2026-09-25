using Cassandra;
using Media.Database.Helpers;
using Media.Database.Mappers;
using Media.Database.Models;
using Media.Database.Repositories.Queries.Helpers;
using Npgsql;

#pragma warning disable CS8981
using cg = Media.Database.Repositories.Schemas.TablesSql.GroupsColumns;
using ccg = Media.Database.Repositories.Schemas.TablesCql.GroupsColumns;
using os = Media.Database.Repositories.Schemas.OrdinalsSql;
using pn = Media.Database.Repositories.Schemas.ParameterNames;
using tc = Media.Database.Repositories.Schemas.TablesCql;
using ts = Media.Database.Repositories.Schemas.TablesSql;
#pragma warning restore CS8981

namespace Media.Database.Repositories.Queries;

/// <summary>
/// SQL and CQL query text, and reader/row mapping extensions, for groups.
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
            {cg.IsEncrypted},
            {cg.InsertedOn},
            {cg.UpdatedOn}
        ;";

    /// <summary>
    /// SQL to select a group by its <c>GroupId</c> -- the PostgreSQL-fallback path for
    /// <see cref="GroupRepository.GetByIdsAsync"/> when Scylla doesn't have the row yet.
    /// </summary>
    public static string GetByIdSql => $@"
        SELECT
            {cg.GroupId},
            {cg.GroupUuid},
            {cg.Name},
            {cg.Title},
            {cg.Description},
            {cg.IsActive},
            {cg.IsEncrypted},
            {cg.InsertedOn},
            {cg.UpdatedOn}
        FROM {ts.Groups}
        WHERE {cg.GroupId} = {pn.GroupId}
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
            {cg.IsEncrypted},
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
            {cg.IsEncrypted},
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
            {cg.IsEncrypted},
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
            {cg.IsEncrypted},
            {cg.InsertedOn},
            {cg.UpdatedOn}
        ;";

    /// <summary>
    /// SQL to set a group's <c>IsEncrypted</c> policy flag, returning the updated row. Group-admin
    /// authorization is enforced by the caller (see GroupService.SetIsEncryptedAsync), not here.
    /// </summary>
    public static string SetIsEncryptedSql => $@"
        UPDATE {ts.Groups} SET
            {cg.IsEncrypted} = {pn.IsEncrypted},
            {cg.UpdatedOn} = {pn.UpdatedOn}
        WHERE {cg.GroupId} = {pn.GroupId}
        RETURNING
            {cg.GroupId},
            {cg.GroupUuid},
            {cg.Name},
            {cg.Title},
            {cg.Description},
            {cg.IsActive},
            {cg.IsEncrypted},
            {cg.InsertedOn},
            {cg.UpdatedOn}
        ;";

    #endregion

    #region CQL Queries

    /// <summary>CQL to select a group by its <c>group_id</c> (Scylla-hydration lookup).</summary>
    public static string GetByIdCql => $@"
        SELECT
            {ccg.GroupId},
            {ccg.GroupUuid},
            {ccg.Name},
            {ccg.Title},
            {ccg.Description},
            {ccg.IsActive},
            {ccg.IsEncrypted},
            {ccg.InsertedOn},
            {ccg.UpdatedOn}
        FROM
            {tc.Groups}
        WHERE
            {ccg.GroupId} = {pn.GroupId}
        LIMIT 1
        ;";

    /// <summary>CQL to insert/replace a group row.</summary>
    public static string UpsertCql => $@"
        INSERT INTO {tc.Groups}
        (
            {ccg.GroupId},
            {ccg.GroupUuid},
            {ccg.Name},
            {ccg.Title},
            {ccg.Description},
            {ccg.IsActive},
            {ccg.IsEncrypted},
            {ccg.InsertedOn},
            {ccg.UpdatedOn}
        )
        VALUES
        (
            {pn.GroupId},
            {pn.GroupUuid},
            {pn.Name},
            {pn.Title},
            {pn.Description},
            {pn.IsActive},
            {pn.IsEncrypted},
            {pn.InsertedOn},
            {pn.UpdatedOn}
        )
        ;";

    /// <summary>CQL to delete a group row by <c>group_id</c>.</summary>
    public static string DeleteCql => $@"
        DELETE FROM {tc.Groups} WHERE {ccg.GroupId} = {pn.GroupId};";

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
            reader.GetStringOrDefault(os.Description),
            reader.GetFieldValue<bool>(os.IsActive),
            reader.GetFieldValue<bool>(os.IsEncrypted),
            reader.GetFieldValue<DateTimeOffset>(os.InsertedOn),
            reader.GetFieldValue<DateTimeOffset?>(os.UpdatedOn));
    }

    /// <summary>Maps a Cassandra/Scylla <paramref name="row"/> to a <see cref="Group"/>.</summary>
    public static Group ToGroup(this Row row)
    {
        return new Group
        {
            GroupId = row.GetValue<int>(ccg.GroupId),
            GroupUuid = row.GetValue<Guid>(ccg.GroupUuid),
            Name = row.GetValue<string>(ccg.Name),
            Title = row.GetValue<string>(ccg.Title),
            Description = row.GetValue<string?>(ccg.Description),
            IsActive = row.GetValue<bool>(ccg.IsActive),
            IsEncrypted = row.GetValue<bool>(ccg.IsEncrypted),
            InsertedOn = row.GetValue<DateTimeOffset>(ccg.InsertedOn),
            UpdatedOn = row.GetValue<DateTimeOffset?>(ccg.UpdatedOn)
        };
    }
}
