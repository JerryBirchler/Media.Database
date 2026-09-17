using Cassandra;
using Media.Database.Models;

#pragma warning disable CS8981
using ccbef = Media.Database.Repositories.Schemas.TablesCql.CanBeEncryptedFieldsColumns;
using pn = Media.Database.Repositories.Schemas.ParameterNames;
using tc = Media.Database.Repositories.Schemas.TablesCql;
#pragma warning restore CS8981

namespace Media.Database.Repositories.Queries;

/// <summary>
/// CQL query text, and row mapping, for the CanBeEncryptedFields registry (MEDIA-12) -- Scylla
/// only, no Postgres counterpart or CDC feed, since it isn't hydrating anything that already
/// lives in Postgres; it's the system of record for its own rows, written directly.
/// </summary>
public static class QueryCanBeEncryptedFields
{
    public static string GetAllSql => $@"
        SELECT
            {ccbef.TableName},
            {ccbef.ColumnName},
            {ccbef.ReleaseIntroduced},
            {ccbef.ReleaseRemoved},
            {ccbef.InsertedOn},
            {ccbef.UpdatedOn}
        FROM {tc.CanBeEncryptedFields}
        ;";

    public static string RegisterSql => $@"
        INSERT INTO {tc.CanBeEncryptedFields}
        (
            {ccbef.TableName},
            {ccbef.ColumnName},
            {ccbef.ReleaseIntroduced},
            {ccbef.InsertedOn}
        )
        VALUES
        (
            {pn.TableName},
            {pn.ColumnName},
            {pn.ReleaseIntroduced},
            {pn.InsertedOn}
        )
        ;";

    public static string SetReleaseRemovedSql => $@"
        UPDATE {tc.CanBeEncryptedFields} SET
            {ccbef.ReleaseRemoved} = {pn.ReleaseRemoved},
            {ccbef.UpdatedOn} = {pn.UpdatedOn}
        WHERE
            {ccbef.TableName} = {pn.TableName} AND {ccbef.ColumnName} = {pn.ColumnName}
        ;";

    public static CanBeEncryptedField ToCanBeEncryptedField(this Row row)
    {
        return new CanBeEncryptedField
        {
            TypeName = row.GetValue<string>(ccbef.TableName),
            MemberName = row.GetValue<string>(ccbef.ColumnName),
            ReleaseIntroduced = row.GetValue<int?>(ccbef.ReleaseIntroduced),
            ReleaseRemoved = row.GetValue<int?>(ccbef.ReleaseRemoved)
        };
    }
}
