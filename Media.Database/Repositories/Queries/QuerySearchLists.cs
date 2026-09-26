using Media.Database.Models;
using Media.Database.Helpers;
using Npgsql;
#pragma warning disable CS8981
using gv = Media.Database.Repositories.Schemas.TablesSql.GroupSearchListsColumns;
using os = Media.Database.Repositories.Schemas.OrdinalsSql;
using pn = Media.Database.Repositories.Schemas.ParameterNames;
using pv = Media.Database.Repositories.Schemas.TablesSql.PersonSearchListsColumns;
using ts = Media.Database.Repositories.Schemas.TablesSql;
#pragma warning restore CS8981

namespace Media.Database.Repositories.Queries;

/// <summary>
/// SQL for the Postgres half of saved search lists. Postgres orchestrates -- identity, owner,
/// type, timestamps -- and Scylla holds the name and the lines, both encrypted.
///
/// Every statement is scoped by owner as well as by uuid, and none may ever be written otherwise.
/// Knowing somebody else's uuid must not grant access to their list: authorization is the owner
/// predicate, not the obscurity of the identifier. The unique index on the uuid carries the owner
/// id in its INCLUDE, so the extra predicate is checked from the index without a heap fetch.
///
/// Two scopes, two tables, and therefore two of everything here. That is the cost of refusing a
/// single table with mutually exclusive nullable foreign keys, and it is paid once, in this file,
/// rather than in every query and every reader's head.
/// </summary>
public static class QuerySearchLists
{
    /// <summary>
    /// SQL to create a person's list. Returns the identity and uuid Postgres issued, which the
    /// caller needs before it can write the blobs to Scylla.
    /// </summary>
    public static string AddPersonSql => $@"
        INSERT INTO {ts.PersonSearchLists} (
            {pv.PersonId},
            {pv.ListType}
        ) VALUES (
            {pn.PersonId},
            {pn.ListType}
        )
        RETURNING
            {pv.PersonSearchListId},
            {pv.PersonSearchListUuid},
            {pv.PersonId},
            {pv.ListType},
            {pv.InsertedOn},
            {pv.UpdatedOn}
        ;";

    /// <summary>SQL to create a group's list.</summary>
    public static string AddGroupSql => $@"
        INSERT INTO {ts.GroupSearchLists} (
            {gv.GroupId},
            {gv.ListType}
        ) VALUES (
            {pn.GroupId},
            {pn.ListType}
        )
        RETURNING
            {gv.GroupSearchListId},
            {gv.GroupSearchListUuid},
            {gv.GroupId},
            {gv.ListType},
            {gv.InsertedOn},
            {gv.UpdatedOn}
        ;";

    /// <summary>
    /// SQL to resolve a person's list by uuid. The owner predicate is what stops a known uuid
    /// being usable by whoever knows it, so it is not optional and never has been.
    /// </summary>
    public static string GetPersonByUuidSql => $@"
        SELECT
            {pv.PersonSearchListId},
            {pv.PersonSearchListUuid},
            {pv.PersonId},
            {pv.ListType},
            {pv.InsertedOn},
            {pv.UpdatedOn}
        FROM {ts.PersonSearchLists}
        WHERE {pv.PersonSearchListUuid} = {pn.PersonSearchListUuid}
          AND {pv.PersonId} = {pn.PersonId}
        ;";

    /// <summary>SQL to resolve a group's list by uuid, scoped to the group.</summary>
    public static string GetGroupByUuidSql => $@"
        SELECT
            {gv.GroupSearchListId},
            {gv.GroupSearchListUuid},
            {gv.GroupId},
            {gv.ListType},
            {gv.InsertedOn},
            {gv.UpdatedOn}
        FROM {ts.GroupSearchLists}
        WHERE {gv.GroupSearchListUuid} = {pn.GroupSearchListUuid}
          AND {gv.GroupId} = {pn.GroupId}
        ;";

    /// <summary>
    /// SQL to list a person's lists, optionally narrowed to one kind. A null list type means all
    /// of them, following the optional-filter pattern already used across the word queries.
    ///
    /// Covered by IX_PersonSearchLists_PersonId_ListType, so the picker reaches Scylla for the
    /// names without ever touching the heap here.
    /// </summary>
    public static string GetPersonListsSql => $@"
        SELECT
            {pv.PersonSearchListId},
            {pv.PersonSearchListUuid},
            {pv.PersonId},
            {pv.ListType},
            {pv.InsertedOn},
            {pv.UpdatedOn}
        FROM {ts.PersonSearchLists}
        WHERE {pv.PersonId} = {pn.PersonId}
          AND ({pn.ListType} IS NULL OR {pv.ListType} = {pn.ListType})
        ORDER BY {pv.PersonSearchListId}
        ;";

    /// <summary>SQL to list a group's lists, optionally narrowed to one kind.</summary>
    public static string GetGroupListsSql => $@"
        SELECT
            {gv.GroupSearchListId},
            {gv.GroupSearchListUuid},
            {gv.GroupId},
            {gv.ListType},
            {gv.InsertedOn},
            {gv.UpdatedOn}
        FROM {ts.GroupSearchLists}
        WHERE {gv.GroupId} = {pn.GroupId}
          AND ({pn.ListType} IS NULL OR {gv.ListType} = {pn.ListType})
        ORDER BY {gv.GroupSearchListId}
        ;";

    /// <summary>
    /// SQL to touch a person's list when its content changes. The row itself carries nothing the
    /// user edits -- the name and the lines are in Scylla -- so this exists to move UpdatedOn and,
    /// by returning the id, to confirm the list is theirs before the blobs are rewritten.
    /// </summary>
    public static string UpdatePersonSql => $@"
        UPDATE {ts.PersonSearchLists} SET
            {pv.ListType} = {pn.ListType},
            {pv.UpdatedOn} = CURRENT_TIMESTAMP(3)
        WHERE {pv.PersonSearchListUuid} = {pn.PersonSearchListUuid}
          AND {pv.PersonId} = {pn.PersonId}
        RETURNING
            {pv.PersonSearchListId},
            {pv.PersonSearchListUuid},
            {pv.PersonId},
            {pv.ListType},
            {pv.InsertedOn},
            {pv.UpdatedOn}
        ;";

    /// <summary>SQL to touch a group's list, scoped to the group.</summary>
    public static string UpdateGroupSql => $@"
        UPDATE {ts.GroupSearchLists} SET
            {gv.ListType} = {pn.ListType},
            {gv.UpdatedOn} = CURRENT_TIMESTAMP(3)
        WHERE {gv.GroupSearchListUuid} = {pn.GroupSearchListUuid}
          AND {gv.GroupId} = {pn.GroupId}
        RETURNING
            {gv.GroupSearchListId},
            {gv.GroupSearchListUuid},
            {gv.GroupId},
            {gv.ListType},
            {gv.InsertedOn},
            {gv.UpdatedOn}
        ;";

    /// <summary>
    /// SQL to delete a person's list. Returns the borrowed id so the caller knows which Scylla row
    /// to remove, and returns nothing at all when the list is not theirs.
    /// </summary>
    public static string DeletePersonSql => $@"
        DELETE FROM {ts.PersonSearchLists}
        WHERE {pv.PersonSearchListUuid} = {pn.PersonSearchListUuid}
          AND {pv.PersonId} = {pn.PersonId}
        RETURNING {pv.PersonSearchListId}
        ;";

    /// <summary>SQL to delete a group's list, scoped to the group.</summary>
    public static string DeleteGroupSql => $@"
        DELETE FROM {ts.GroupSearchLists}
        WHERE {gv.GroupSearchListUuid} = {pn.GroupSearchListUuid}
          AND {gv.GroupId} = {pn.GroupId}
        RETURNING {gv.GroupSearchListId}
        ;";
}

/// <summary>Row mapping for <see cref="QuerySearchLists"/>.</summary>
public static class SearchListMappers
{
    /// <summary>
    /// Maps a person's skeleton row. The name and the lines are absent by design -- they live in
    /// Scylla, and this half of the list has deliberately never seen them.
    /// </summary>
    public static SearchList ToPersonSearchList(this NpgsqlDataReader reader)
    {
        return new SearchList
        {
            Id = reader.GetInt32(os.PersonSearchListId),
            Uuid = reader.GetGuid(os.PersonSearchListUuid),
            Scope = OwnerScope.Person,
            OwnerId = reader.GetInt32(os.PersonId),
            ListType = (SearchListType)reader.GetInt32(os.ListType),
            InsertedOn = reader.GetFieldValue<DateTimeOffset>(os.InsertedOn),
            UpdatedOn = reader.GetFieldValue<DateTimeOffset?>(os.UpdatedOn)
        };
    }

    /// <summary>Maps a group's skeleton row.</summary>
    public static SearchList ToGroupSearchList(this NpgsqlDataReader reader)
    {
        return new SearchList
        {
            Id = reader.GetInt32(os.GroupSearchListId),
            Uuid = reader.GetGuid(os.GroupSearchListUuid),
            Scope = OwnerScope.Group,
            OwnerId = reader.GetInt32(os.GroupId),
            ListType = (SearchListType)reader.GetInt32(os.ListType),
            InsertedOn = reader.GetFieldValue<DateTimeOffset>(os.InsertedOn),
            UpdatedOn = reader.GetFieldValue<DateTimeOffset?>(os.UpdatedOn)
        };
    }
}
