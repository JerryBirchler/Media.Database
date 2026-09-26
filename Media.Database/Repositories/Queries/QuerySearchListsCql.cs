using Cassandra;
using Media.Database.Models;
#pragma warning disable CS8981
using cg = Media.Database.Repositories.Schemas.TablesCql.GroupSearchListsColumns;
using cp = Media.Database.Repositories.Schemas.TablesCql.PersonSearchListsColumns;
using pn = Media.Database.Repositories.Schemas.ParameterNames;
using tc = Media.Database.Repositories.Schemas.TablesCql;
#pragma warning restore CS8981

namespace Media.Database.Repositories.Queries;

/// <summary>
/// CQL for the content of saved search lists (API-115) -- the name and the lines, both encrypted.
/// Postgres holds the skeleton and issues the identity; this side holds everything the user wrote.
///
/// The partition key is the owner, which makes the storage shape itself the authorization
/// boundary: a read cannot cross owners even if somebody knows another person's list id. That is
/// a property worth preserving, so no query here is ever keyed on the id alone.
///
/// Both content columns are <c>text</c> rather than <c>blob</c>. Encryptor returns base64 with the
/// AES-GCM nonce and tag already embedded, so there is nothing to keep as raw bytes and no
/// separate nonce column -- the correction group_uuid_orchestration made in Scylla migration
/// 000010.
/// </summary>
public static class QuerySearchListsCql
{
    /// <summary>CQL to write, or replace, the content of a person's list.</summary>
    public static string UpsertPersonSql => $@"
        INSERT INTO {tc.PersonSearchLists}
        (
            {cp.PersonId},
            {cp.PersonSearchListId},
            {cp.Name},
            {cp.Payload},
            {cp.PayloadVersion}
        )
        VALUES
        (
            {pn.PersonId},
            {pn.PersonSearchListId},
            {pn.Name},
            {pn.Payload},
            {pn.PayloadVersion}
        )
        ;";

    /// <summary>CQL to write, or replace, the content of a group's list.</summary>
    public static string UpsertGroupSql => $@"
        INSERT INTO {tc.GroupSearchLists}
        (
            {cg.GroupId},
            {cg.GroupSearchListId},
            {cg.Name},
            {cg.Payload},
            {cg.PayloadVersion}
        )
        VALUES
        (
            {pn.GroupId},
            {pn.GroupSearchListId},
            {pn.Name},
            {pn.Payload},
            {pn.PayloadVersion}
        )
        ;";

    /// <summary>CQL to read one of a person's lists.</summary>
    public static string GetPersonSql => $@"
        SELECT
            {cp.PersonSearchListId},
            {cp.Name},
            {cp.Payload},
            {cp.PayloadVersion}
        FROM {tc.PersonSearchLists}
        WHERE {cp.PersonId} = {pn.PersonId}
          AND {cp.PersonSearchListId} = {pn.PersonSearchListId}
        ;";

    /// <summary>CQL to read one of a group's lists.</summary>
    public static string GetGroupSql => $@"
        SELECT
            {cg.GroupSearchListId},
            {cg.Name},
            {cg.Payload},
            {cg.PayloadVersion}
        FROM {tc.GroupSearchLists}
        WHERE {cg.GroupId} = {pn.GroupId}
          AND {cg.GroupSearchListId} = {pn.GroupSearchListId}
        ;";

    /// <summary>
    /// CQL to read every list a person owns, in one partition read. This is what the picker needs:
    /// twenty names, one round trip, rather than a point read each.
    /// </summary>
    public static string GetAllForPersonSql => $@"
        SELECT
            {cp.PersonSearchListId},
            {cp.Name},
            {cp.Payload},
            {cp.PayloadVersion}
        FROM {tc.PersonSearchLists}
        WHERE {cp.PersonId} = {pn.PersonId}
        ;";

    /// <summary>CQL to read every list a group owns, in one partition read.</summary>
    public static string GetAllForGroupSql => $@"
        SELECT
            {cg.GroupSearchListId},
            {cg.Name},
            {cg.Payload},
            {cg.PayloadVersion}
        FROM {tc.GroupSearchLists}
        WHERE {cg.GroupId} = {pn.GroupId}
        ;";

    /// <summary>CQL to remove a person's list content.</summary>
    public static string DeletePersonSql => $@"
        DELETE FROM {tc.PersonSearchLists}
        WHERE {cp.PersonId} = {pn.PersonId}
          AND {cp.PersonSearchListId} = {pn.PersonSearchListId}
        ;";

    /// <summary>CQL to remove a group's list content.</summary>
    public static string DeleteGroupSql => $@"
        DELETE FROM {tc.GroupSearchLists}
        WHERE {cg.GroupId} = {pn.GroupId}
          AND {cg.GroupSearchListId} = {pn.GroupSearchListId}
        ;";

    /// <summary>Maps a person's row to its still-encrypted content.</summary>
    public static SearchListContent ToPersonSearchListContent(this Row row)
    {
        return new SearchListContent
        {
            Id = row.GetValue<int>(cp.PersonSearchListId),
            Name = row.GetValue<string>(cp.Name),
            Payload = row.GetValue<string>(cp.Payload),
            PayloadVersion = row.GetValue<int>(cp.PayloadVersion)
        };
    }

    /// <summary>Maps a group's row to its still-encrypted content.</summary>
    public static SearchListContent ToGroupSearchListContent(this Row row)
    {
        return new SearchListContent
        {
            Id = row.GetValue<int>(cg.GroupSearchListId),
            Name = row.GetValue<string>(cg.Name),
            Payload = row.GetValue<string>(cg.Payload),
            PayloadVersion = row.GetValue<int>(cg.PayloadVersion)
        };
    }
}
