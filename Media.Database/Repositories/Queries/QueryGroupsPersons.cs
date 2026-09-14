using Media.Database.Helpers;
using Media.Database.Mappers;
using Media.Database.Models;
using Npgsql;

#pragma warning disable CS8981
using cg = Media.Database.Repositories.Schemas.TablesSql.GroupsColumns;
using cgp = Media.Database.Repositories.Schemas.TablesSql.GroupsPersonsColumns;
using cp = Media.Database.Repositories.Schemas.TablesSql.PersonsColumns;
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

    /// <summary>
    /// SQL to select just the ordering key (GroupId, Name) for a keyset-paged page of the active
    /// groups a person belongs to, ordered by name -- cheap enough to identify from PostgreSQL
    /// alone before hydrating full rows from Scylla (with a PostgreSQL fallback) via
    /// <see cref="IGroupRepository.GetByIdsAsync"/>. GroupId is included in the cursor tuple purely
    /// as a defensive tiebreaker (Name already carries a unique index) rather than because ties are
    /// expected in practice.
    /// </summary>
    public static string GetGroupIdentifiersByPersonIdSql => $@"
        SELECT
            g.{cg.GroupId},
            g.{cg.Name}
        FROM
            {ts.Groups} AS g
        JOIN
            {ts.GroupsPersons} AS gp ON gp.{cgp.GroupId} = g.{cg.GroupId}
        WHERE
            gp.{cgp.PersonId} = {pn.PersonId}
            AND gp.{cgp.IsActive} = true
            AND g.{cg.IsActive} = true
            AND (g.{cg.Name}, g.{cg.GroupId}) >
            (
                COALESCE({pn.Name}, ''),
                COALESCE({pn.GroupId}, 0)
            )
        ORDER BY
            g.{cg.Name} ASC,
            g.{cg.GroupId} ASC
        LIMIT {pn.Limit}
        ;";

    /// <summary>
    /// SQL to select just the ordering key (PersonId, PersonUuid, LastName, FirstName) for a
    /// keyset-paged page of a group's active members, ordered by last name then first name (ties
    /// broken by the externally-facing PersonUuid, not the internal PersonId) -- cheap enough to
    /// identify from PostgreSQL alone before hydrating full rows from Scylla (with a PostgreSQL
    /// fallback) via <see cref="IPersonRepository.GetByIdsAsync"/>. Deliberately does not filter on
    /// Persons.IsActive -- this endpoint is group-admin-only (MEDIA-8), and shows active and
    /// inactivated members alike, same as a person's own creator sees both via GET /api/persons.
    /// </summary>
    public static string GetPersonIdentifiersByGroupIdSql => $@"
        SELECT
            p.{cp.PersonId},
            p.{cp.PersonUuid},
            p.{cp.LastName},
            p.{cp.FirstName}
        FROM
            {ts.Persons} AS p
        JOIN
            {ts.GroupsPersons} AS gp ON gp.{cgp.PersonId} = p.{cp.PersonId}
        WHERE
            gp.{cgp.GroupId} = {pn.GroupId}
            AND gp.{cgp.IsActive} = true
            AND (p.{cp.LastName}, p.{cp.FirstName}, p.{cp.PersonUuid}) >
            (
                COALESCE({pn.LastName}, ''),
                COALESCE({pn.FirstName}, ''),
                COALESCE({pn.PersonUuid}, '00000000-0000-0000-0000-000000000000'::uuid)
            )
        ORDER BY
            p.{cp.LastName} ASC,
            p.{cp.FirstName} ASC,
            p.{cp.PersonUuid} ASC
        LIMIT {pn.Limit}
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

    /// <summary>Maps the current row of <paramref name="reader"/> to its GroupId and Name only.</summary>
    public static (int GroupId, string Name) ToGroupIdentifier(this NpgsqlDataReader reader)
    {
        return (GroupId: reader.GetInt32(os.GroupId), Name: reader.GetString(os.Name));
    }

    /// <summary>Reads every remaining row from <paramref name="reader"/> and maps each to its GroupId and Name.</summary>
    public static async Task<List<(int GroupId, string Name)>> ToGroupIdentifiers(this NpgsqlDataReader reader)
    {
        List<(int GroupId, string Name)> identifiers = [];

        while (await reader.ReadAsync())
            identifiers.Add(reader.ToGroupIdentifier());

        return identifiers;
    }

    /// <summary>Maps the current row of <paramref name="reader"/> to a <see cref="PersonIdentifier"/>.</summary>
    public static PersonIdentifier ToPersonIdentifier(this NpgsqlDataReader reader)
    {
        return new PersonIdentifier
        {
            PersonId = reader.GetInt32(os.PersonId),
            PersonUuid = reader.GetGuid(os.PersonUuid),
            LastName = reader.GetString(os.LastName),
            FirstName = reader.GetString(os.FirstName)
        };
    }

    /// <summary>Reads every remaining row from <paramref name="reader"/> and maps each to a <see cref="PersonIdentifier"/>.</summary>
    public static async Task<List<PersonIdentifier>> ToPersonIdentifiers(this NpgsqlDataReader reader)
    {
        List<PersonIdentifier> identifiers = [];

        while (await reader.ReadAsync())
            identifiers.Add(reader.ToPersonIdentifier());

        return identifiers;
    }
}
