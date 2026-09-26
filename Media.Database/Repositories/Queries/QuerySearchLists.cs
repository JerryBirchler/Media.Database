#pragma warning disable CS8981
using pn = Media.Database.Repositories.Schemas.ParameterNames;
#pragma warning restore CS8981

namespace Media.Database.Repositories.Queries;

/// <summary>
/// SQL for the Postgres half of saved search lists. Postgres orchestrates -- identity, owner,
/// type, timestamps -- and Scylla holds the name and the lines, both encrypted.
///
/// Written once and parameterized by scope rather than three times over. A device, a person and a
/// group each own their lists in their own table pair, but the statements are identical apart
/// from the identifiers, which come from <see cref="SearchListScopeSchema"/>. Two scopes already
/// meant two of everything here; a third would have cemented it.
///
/// Every statement is scoped by owner as well as by uuid, and none may ever be written otherwise.
/// Knowing somebody else's uuid must not grant access to their list: authorization is the owner
/// predicate, not the obscurity of the identifier. The unique index on the uuid carries the owner
/// id in its INCLUDE, so the extra predicate is checked from the index without a heap fetch.
/// </summary>
internal static class QuerySearchLists
{
    /// <summary>
    /// SQL to create a list. Returns the identity and uuid Postgres issued, which the caller
    /// needs before it can write the blobs to Scylla.
    /// </summary>
    public static string Add(SearchListScopeSchema s) => $@"
        INSERT INTO {s.Table} (
            {s.OwnerColumn},
            {s.ListTypeColumn}
        ) VALUES (
            {s.OwnerParameter},
            {pn.ListType}
        )
        RETURNING {Columns(s)}
        ;";

    /// <summary>
    /// SQL to resolve one list by uuid. The owner predicate is what stops a known uuid being
    /// usable by whoever knows it, so it is not optional and never has been.
    /// </summary>
    public static string GetByUuid(SearchListScopeSchema s) => $@"
        SELECT {Columns(s)}
        FROM {s.Table}
        WHERE {s.UuidColumn} = {s.UuidParameter}
          AND {s.OwnerColumn} = {s.OwnerParameter}
        ;";

    /// <summary>
    /// SQL to list an owner's lists, optionally narrowed to one kind. A null list type means all
    /// of them, following the optional-filter pattern already used across the word queries.
    ///
    /// The cast is not decoration: a null passed as DBNull gives Npgsql no type to infer, and
    /// Postgres rejects the statement rather than treating it as "any".
    ///
    /// Covered by the (Owner, ListType) index, so the picker reaches Scylla for the names without
    /// ever touching the heap here.
    /// </summary>
    public static string GetAll(SearchListScopeSchema s) => $@"
        SELECT {Columns(s)}
        FROM {s.Table}
        WHERE {s.OwnerColumn} = {s.OwnerParameter}
          AND ({pn.ListType}::int IS NULL OR {s.ListTypeColumn} = {pn.ListType}::int)
        ORDER BY {s.IdColumn}
        ;";

    /// <summary>
    /// SQL to touch a list when its content changes. The row itself carries nothing the user
    /// edits -- the name and the lines are in Scylla -- so this exists to move UpdatedOn and, by
    /// returning the id, to confirm the list is the owner's before the blobs are rewritten.
    /// </summary>
    public static string Update(SearchListScopeSchema s) => $@"
        UPDATE {s.Table} SET
            {s.ListTypeColumn} = {pn.ListType},
            {s.UpdatedOnColumn} = CURRENT_TIMESTAMP(3)
        WHERE {s.UuidColumn} = {s.UuidParameter}
          AND {s.OwnerColumn} = {s.OwnerParameter}
        RETURNING {Columns(s)}
        ;";

    /// <summary>
    /// SQL to delete a list. Returns the borrowed id so the caller knows which Scylla row to
    /// remove, and returns nothing at all when the list is not the owner's.
    /// </summary>
    public static string Delete(SearchListScopeSchema s) => $@"
        DELETE FROM {s.Table}
        WHERE {s.UuidColumn} = {s.UuidParameter}
          AND {s.OwnerColumn} = {s.OwnerParameter}
        RETURNING {s.IdColumn}
        ;";

    private static string Columns(SearchListScopeSchema s) =>
        $"{s.IdColumn}, {s.UuidColumn}, {s.OwnerColumn}, {s.ListTypeColumn}, {s.InsertedOnColumn}, {s.UpdatedOnColumn}";
}
