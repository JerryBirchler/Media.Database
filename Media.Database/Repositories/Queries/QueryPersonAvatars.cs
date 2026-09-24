using Cassandra;
using Media.Database.Models;
#pragma warning disable CS8981
using cpa = Media.Database.Repositories.Schemas.TablesCql.PersonAvatarsColumns;
using pn = Media.Database.Repositories.Schemas.ParameterNames;
using tc = Media.Database.Repositories.Schemas.TablesCql;
#pragma warning restore CS8981

namespace Media.Database.Repositories.Queries;

/// <summary>
/// CQL query text, and row mapping, for the person_avatars table (MEDIA-40) -- Scylla only, no
/// Postgres counterpart or CDC feed: it is the system of record for its own rows.
/// </summary>
public static class QueryPersonAvatars
{
    /// <summary>CQL to select one person's picture.</summary>
    public static string GetSql => $@"
        SELECT
            {cpa.PersonId},
            {cpa.ContentType},
            {cpa.Image},
            {cpa.UpdatedOn}
        FROM {tc.PersonAvatars}
        WHERE {cpa.PersonId} = {pn.PersonId}
        ;";

    /// <summary>CQL to write (or replace) a person's picture.</summary>
    public static string UpsertSql => $@"
        INSERT INTO {tc.PersonAvatars}
        (
            {cpa.PersonId},
            {cpa.ContentType},
            {cpa.Image},
            {cpa.UpdatedOn}
        )
        VALUES
        (
            {pn.PersonId},
            {pn.ContentType},
            {pn.Image},
            {pn.UpdatedOn}
        )
        ;";

    /// <summary>CQL to remove a person's picture.</summary>
    public static string DeleteSql => $@"
        DELETE FROM {tc.PersonAvatars}
        WHERE {cpa.PersonId} = {pn.PersonId}
        ;";

    /// <summary>Maps the current <paramref name="row"/> to a <see cref="PersonAvatar"/>.</summary>
    public static PersonAvatar ToPersonAvatar(this Row row)
    {
        return new PersonAvatar
        {
            PersonId = row.GetValue<int>(cpa.PersonId),
            ContentType = row.GetValue<string>(cpa.ContentType),
            Image = row.GetValue<byte[]>(cpa.Image),
            UpdatedOn = row.GetValue<DateTimeOffset>(cpa.UpdatedOn)
        };
    }
}
