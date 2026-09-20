using Cassandra;
using Media.Database.Models;

#pragma warning disable CS8981
using cguo = Media.Database.Repositories.Schemas.TablesCql.GroupUuidOrchestrationColumns;
using pn = Media.Database.Repositories.Schemas.ParameterNames;
using tc = Media.Database.Repositories.Schemas.TablesCql;
#pragma warning restore CS8981

namespace Media.Database.Repositories.Queries;

/// <summary>
/// CQL query text, and row mapping, for the group_uuid_orchestration table (MEDIA-36) -- Scylla
/// only, no Postgres counterpart or CDC feed, since it isn't hydrating anything that already
/// lives in Postgres; it's the system of record for its own rows, written directly.
/// </summary>
public static class QueryGroupUuidOrchestration
{
    /// <summary>SQL to write (or overwrite) a person's orchestration entry for one group.</summary>
    public static string UpsertSql => $@"
        INSERT INTO {tc.GroupUuidOrchestration}
        (
            {cguo.PersonUuid},
            {cguo.GroupShellId},
            {cguo.EncryptedUuidBlob},
            {cguo.GroupEncryptionKeyUuid},
            {cguo.UpdatedOn}
        )
        VALUES
        (
            {pn.PersonUuid},
            {pn.GroupShellId},
            {pn.EncryptedUuidBlob},
            {pn.GroupEncryptionKeyUuid},
            {pn.UpdatedOn}
        )
        ;";

    /// <summary>
    /// CQL to select every orchestration entry for a person, across every group they belong to --
    /// the partition query a verified person's own client uses to fetch its own entries.
    /// </summary>
    public static string GetAllByPersonUuidSql => $@"
        SELECT
            {cguo.PersonUuid},
            {cguo.GroupShellId},
            {cguo.EncryptedUuidBlob},
            {cguo.GroupEncryptionKeyUuid},
            {cguo.UpdatedOn}
        FROM {tc.GroupUuidOrchestration}
        WHERE {cguo.PersonUuid} = {pn.PersonUuid}
        ;";

    /// <summary>Maps the current <paramref name="row"/> to a <see cref="GroupUuidOrchestration"/>.</summary>
    public static GroupUuidOrchestration ToGroupUuidOrchestration(this Row row)
    {
        return new GroupUuidOrchestration
        {
            PersonUuid = row.GetValue<Guid>(cguo.PersonUuid),
            GroupShellId = row.GetValue<int>(cguo.GroupShellId),
            EncryptedUuidBlob = row.GetValue<string>(cguo.EncryptedUuidBlob),
            GroupEncryptionKeyUuid = row.GetValue<Guid>(cguo.GroupEncryptionKeyUuid),
            UpdatedOn = row.GetValue<DateTimeOffset>(cguo.UpdatedOn)
        };
    }
}
