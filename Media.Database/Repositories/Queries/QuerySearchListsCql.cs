#pragma warning disable CS8981
using pn = Media.Database.Repositories.Schemas.ParameterNames;
#pragma warning restore CS8981

namespace Media.Database.Repositories.Queries;

/// <summary>
/// CQL for the content of saved search lists -- the name and the lines, both encrypted. Postgres
/// holds the skeleton and issues the identity; this side holds everything the user wrote.
///
/// The partition key is the owner, which makes the storage shape itself the authorization
/// boundary: a read cannot cross owners even if somebody knows another owner's list id. That is a
/// property worth preserving, so no query here is ever keyed on the id alone.
///
/// Both content columns are text rather than blob. Encryptor returns base64 with the AES-GCM
/// nonce and tag already embedded, so there is nothing to keep as raw bytes and no separate nonce
/// column -- the correction group_uuid_orchestration made in Scylla migration 000010.
/// </summary>
internal static class QuerySearchListsCql
{
    /// <summary>CQL to write, or replace, a list's content.</summary>
    public static string Upsert(SearchListScopeSchema s) => $@"
        INSERT INTO {s.CqlTable}
        (
            {s.CqlOwnerColumn},
            {s.CqlIdColumn},
            {s.CqlNameColumn},
            {s.CqlPayloadColumn},
            {s.CqlPayloadVersionColumn}
        )
        VALUES
        (
            {s.OwnerParameter},
            {s.IdParameter},
            {pn.Name},
            {pn.Payload},
            {pn.PayloadVersion}
        )
        ;";

    /// <summary>CQL to read one list's content.</summary>
    public static string Get(SearchListScopeSchema s) => $@"
        SELECT {Columns(s)}
        FROM {s.CqlTable}
        WHERE {s.CqlOwnerColumn} = {s.OwnerParameter}
          AND {s.CqlIdColumn} = {s.IdParameter}
        ;";

    /// <summary>
    /// CQL to read every list an owner has, in one partition read. This is what the picker needs:
    /// twenty names, one round trip, rather than a point read each.
    /// </summary>
    public static string GetAll(SearchListScopeSchema s) => $@"
        SELECT {Columns(s)}
        FROM {s.CqlTable}
        WHERE {s.CqlOwnerColumn} = {s.OwnerParameter}
        ;";

    /// <summary>CQL to remove a list's content.</summary>
    public static string Delete(SearchListScopeSchema s) => $@"
        DELETE FROM {s.CqlTable}
        WHERE {s.CqlOwnerColumn} = {s.OwnerParameter}
          AND {s.CqlIdColumn} = {s.IdParameter}
        ;";

    private static string Columns(SearchListScopeSchema s) =>
        $"{s.CqlIdColumn}, {s.CqlNameColumn}, {s.CqlPayloadColumn}, {s.CqlPayloadVersionColumn}";
}
