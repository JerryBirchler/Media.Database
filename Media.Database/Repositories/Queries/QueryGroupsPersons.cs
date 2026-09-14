using Media.Database.Helpers;
using Media.Database.Mappers;
using Media.Database.Models;
using Npgsql;

#pragma warning disable CS8981
using cgp = Media.Database.Repositories.Schemas.TablesSql.GroupsPersonsColumns;
using os = Media.Database.Repositories.Schemas.OrdinalsSql;
using pn = Media.Database.Repositories.Schemas.ParameterNames;
using ts = Media.Database.Repositories.Schemas.TablesSql;
#pragma warning restore CS8981

namespace Media.Database.Repositories.Queries;

/// <summary>
/// SQL query text, and reader/row mapping extensions, for group/person associations. The
/// partial-unique-index design on <c>GroupsPersons</c> (unique on (GroupId, PersonId) only while
/// active) is built for a deactivate-and-reactivate lifecycle -- <see cref="UpsertSql"/> reflects
/// that by reactivating any existing row (active or not) for the pair rather than ever inserting
/// a duplicate.
/// </summary>
public static class QueryGroupsPersons
{
    #region SQL Queries

    /// <summary>
    /// SQL to upsert a group/person association: reactivates (and updates <c>IsAdmin</c> on) any
    /// existing row for the pair, active or not, or inserts a new active row if none exists.
    /// </summary>
    public static string UpsertSql => $@"
        WITH existing AS (
            SELECT {cgp.GroupPersonId}
            FROM {ts.GroupsPersons}
            WHERE {cgp.GroupId} = {pn.GroupId} AND {cgp.PersonId} = {pn.PersonId}
            LIMIT 1
        ),
        updated AS (
            UPDATE {ts.GroupsPersons} SET
                {cgp.IsActive} = true,
                {cgp.IsAdmin} = {pn.IsAdmin},
                {cgp.UpdatedOn} = {pn.UpdatedOn}
            WHERE {cgp.GroupPersonId} IN (SELECT {cgp.GroupPersonId} FROM existing)
            RETURNING
                {cgp.GroupPersonId}, {cgp.GroupPersonUuid}, {cgp.GroupId}, {cgp.PersonId},
                {cgp.IsActive}, {cgp.IsAdmin}, {cgp.InsertedOn}, {cgp.UpdatedOn}
        ),
        inserted AS (
            INSERT INTO {ts.GroupsPersons} ({cgp.GroupId}, {cgp.PersonId}, {cgp.IsActive}, {cgp.IsAdmin})
            SELECT {pn.GroupId}, {pn.PersonId}, true, {pn.IsAdmin}
            WHERE NOT EXISTS (SELECT 1 FROM existing)
            RETURNING
                {cgp.GroupPersonId}, {cgp.GroupPersonUuid}, {cgp.GroupId}, {cgp.PersonId},
                {cgp.IsActive}, {cgp.IsAdmin}, {cgp.InsertedOn}, {cgp.UpdatedOn}
        )
        SELECT * FROM updated
        UNION ALL
        SELECT * FROM inserted
        ;";

    /// <summary>
    /// SQL to select the active association for a (GroupId, PersonId) pair, or no rows if none is active.
    /// </summary>
    public static string GetActiveSql => $@"
        SELECT
            {cgp.GroupPersonId}, {cgp.GroupPersonUuid}, {cgp.GroupId}, {cgp.PersonId},
            {cgp.IsActive}, {cgp.IsAdmin}, {cgp.InsertedOn}, {cgp.UpdatedOn}
        FROM {ts.GroupsPersons}
        WHERE {cgp.GroupId} = {pn.GroupId} AND {cgp.PersonId} = {pn.PersonId} AND {cgp.IsActive} = true
        ;";

    /// <summary>
    /// SQL to deactivate the active association for a (GroupId, PersonId) pair, returning the
    /// updated row, or no rows if none was active.
    /// </summary>
    public static string DeactivateSql => $@"
        UPDATE {ts.GroupsPersons} SET
            {cgp.IsActive} = false,
            {cgp.UpdatedOn} = {pn.UpdatedOn}
        WHERE {cgp.GroupId} = {pn.GroupId} AND {cgp.PersonId} = {pn.PersonId} AND {cgp.IsActive} = true
        RETURNING
            {cgp.GroupPersonId}, {cgp.GroupPersonUuid}, {cgp.GroupId}, {cgp.PersonId},
            {cgp.IsActive}, {cgp.IsAdmin}, {cgp.InsertedOn}, {cgp.UpdatedOn}
        ;";

    /// <summary>
    /// SQL to count how many active admins a group currently has -- the "at least one admin must
    /// remain" floor check.
    /// </summary>
    public static string CountActiveAdminsSql => $@"
        SELECT COUNT(*)
        FROM {ts.GroupsPersons}
        WHERE {cgp.GroupId} = {pn.GroupId} AND {cgp.IsActive} = true AND {cgp.IsAdmin} = true
        ;";

    #endregion

    /// <summary>
    /// Maps the current row of <paramref name="reader"/> to a <see cref="GroupPerson"/>, via
    /// <paramref name="mapper"/>.
    /// </summary>
    public static GroupPerson ToGroupPerson(this NpgsqlDataReader reader, IMapGroupPersonResponse mapper)
    {
        return mapper.ToGroupPerson(
            reader.GetInt32(os.GroupPersonId),
            reader.GetGuid(os.GroupPersonUuid),
            reader.GetInt32(os.GroupId),
            reader.GetInt32(os.PersonId),
            reader.GetFieldValue<bool>(os.IsActive),
            reader.GetFieldValue<bool>(os.IsAdmin),
            reader.GetFieldValue<DateTimeOffset>(os.InsertedOn),
            reader.GetFieldValue<DateTimeOffset?>(os.UpdatedOn));
    }
}
