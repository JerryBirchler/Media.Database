using Media.Database.Helpers;
using Media.Database.Models;
using Npgsql;

#pragma warning disable CS8981
using cs = Media.Database.Repositories.Schemas.TablesSql.PersonMemberAddStrikesColumns;
using ccs = Media.Database.Repositories.Schemas.TablesCql.PersonMemberAddStrikesColumns;
using os = Media.Database.Repositories.Schemas.OrdinalsSql;
using pn = Media.Database.Repositories.Schemas.ParameterNames;
using tc = Media.Database.Repositories.Schemas.TablesCql;
using ts = Media.Database.Repositories.Schemas.TablesSql;
#pragma warning restore CS8981

namespace Media.Database.Repositories.Queries;

/// <summary>
/// SQL and CQL for the misses an admin has made adding members by details (SCHEMA-35, MEDIA-8).
///
/// The strike rule lives in <see cref="MemberAddStrikePolicy"/>, not here: these statements only
/// store its result. A miss is written compare-and-set -- the first one inserts only if there is no
/// row yet, later ones update only if the row is still what the policy was given -- so two misses at
/// once can never both count from the same state; the loser reads again and reapplies the policy.
/// </summary>
public static class QueryPersonMemberAddStrikes
{
    private static string Columns => $@"
            {cs.PersonId},
            {cs.StrikeCount},
            {cs.WindowEndsOn},
            {cs.LockedOn},
            {cs.InsertedOn},
            {cs.UpdatedOn}";

    #region SQL Queries

    /// <summary>SQL to read a person's strikes.</summary>
    public static string GetByPersonIdSql => $@"
        SELECT{Columns}
        FROM {ts.PersonMemberAddStrikes}
        WHERE {cs.PersonId} = {pn.PersonId}
        ;";

    /// <summary>
    /// SQL to record a person's first strikes row. Returns no row when one appeared meanwhile.
    /// </summary>
    public static string InsertIfAbsentSql => $@"
        INSERT INTO {ts.PersonMemberAddStrikes} (
            {cs.PersonId},
            {cs.StrikeCount},
            {cs.WindowEndsOn},
            {cs.LockedOn},
            {cs.InsertedOn}
        ) VALUES (
            {pn.PersonId},
            {pn.StrikeCount},
            {pn.WindowEndsOn},
            {pn.LockedOn},
            {pn.Now}
        )
        ON CONFLICT ({cs.PersonId}) DO NOTHING
        RETURNING{Columns}
        ;";

    /// <summary>
    /// SQL to store new strikes only if the row still holds the count and window they were computed
    /// from. Returns no row when it changed meanwhile.
    /// </summary>
    public static string UpdateIfUnchangedSql => $@"
        UPDATE {ts.PersonMemberAddStrikes} SET
            {cs.StrikeCount} = {pn.StrikeCount},
            {cs.WindowEndsOn} = {pn.WindowEndsOn},
            {cs.LockedOn} = {pn.LockedOn},
            {cs.UpdatedOn} = {pn.Now}
        WHERE
            {cs.PersonId} = {pn.PersonId}
            AND {cs.StrikeCount} = {pn.ExpectedStrikeCount}
            AND {cs.WindowEndsOn} IS NOT DISTINCT FROM {pn.ExpectedWindowEndsOn}
        RETURNING{Columns}
        ;";

    /// <summary>
    /// SQL for a successful registration: zeroes the count and clears the lock. Returns no row when
    /// the person has no strikes recorded.
    /// </summary>
    public static string ClearSql => $@"
        UPDATE {ts.PersonMemberAddStrikes} SET
            {cs.StrikeCount} = 0,
            {cs.WindowEndsOn} = NULL,
            {cs.LockedOn} = NULL,
            {cs.UpdatedOn} = {pn.Now}
        WHERE {cs.PersonId} = {pn.PersonId}
        RETURNING{Columns}
        ;";

    /// <summary>
    /// SQL for the reset sweep: zeroes every count whose window has ended. Never touches
    /// <c>LockedOn</c> -- only re-registration clears a lock.
    /// </summary>
    public static string ResetEndedWindowsSql => $@"
        UPDATE {ts.PersonMemberAddStrikes} SET
            {cs.StrikeCount} = 0,
            {cs.WindowEndsOn} = NULL,
            {cs.UpdatedOn} = {pn.Now}
        WHERE
            {cs.StrikeCount} > 0
            AND {cs.WindowEndsOn} <= {pn.Now}
        ;";

    #endregion

    #region CQL Queries

    /// <summary>CQL to insert/replace a person's strikes' Scylla copy.</summary>
    public static string UpsertCql => $@"
        INSERT INTO {tc.PersonMemberAddStrikes}
        (
            {ccs.PersonId},
            {ccs.StrikeCount},
            {ccs.WindowEndsOn},
            {ccs.LockedOn},
            {ccs.InsertedOn},
            {ccs.UpdatedOn}
        )
        VALUES
        (
            {pn.PersonId},
            {pn.StrikeCount},
            {pn.WindowEndsOn},
            {pn.LockedOn},
            {pn.InsertedOn},
            {pn.UpdatedOn}
        );";

    /// <summary>CQL to delete a person's strikes' Scylla copy.</summary>
    public static string DeleteCql => $@"
        DELETE FROM {tc.PersonMemberAddStrikes} WHERE {ccs.PersonId} = {pn.PersonId};";

    #endregion

    /// <summary>Maps the current row of <paramref name="reader"/> to <see cref="PersonMemberAddStrikes"/>.</summary>
    public static PersonMemberAddStrikes ToPersonMemberAddStrikes(this NpgsqlDataReader reader)
    {
        return new PersonMemberAddStrikes
        {
            PersonId = reader.GetInt32(os.PersonId),
            StrikeCount = reader.GetInt32(os.StrikeCount),
            WindowEndsOn = reader.GetFieldValue<DateTimeOffset?>(os.WindowEndsOn),
            LockedOn = reader.GetFieldValue<DateTimeOffset?>(os.LockedOn),
            InsertedOn = reader.GetFieldValue<DateTimeOffset>(os.InsertedOn),
            UpdatedOn = reader.GetFieldValue<DateTimeOffset?>(os.UpdatedOn)
        };
    }
}
