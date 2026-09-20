using Media.Database.Helpers;
using Media.Database.Models;
using Npgsql;

#pragma warning disable CS8981
using cgek = Media.Database.Repositories.Schemas.TablesSql.GroupEncryptionKeysColumns;
using os = Media.Database.Repositories.Schemas.OrdinalsSql;
using pn = Media.Database.Repositories.Schemas.ParameterNames;
using ts = Media.Database.Repositories.Schemas.TablesSql;
#pragma warning restore CS8981

namespace Media.Database.Repositories.Queries;

/// <summary>
/// SQL query text, and reader mapping extensions, for a group's wrapped Data Encryption Keys
/// (MEDIA-35). The wrapped DEK column holds <c>Media.Common.Serialization.Encryptor</c>'s own
/// self-contained output -- never the raw DEK or the group's raw key.
/// </summary>
public static class QueryGroupEncryptionKeys
{
    #region SQL Queries

    /// <summary>SQL to insert a new active key for a (GroupShellId, DataCategory) pair.</summary>
    public static string InsertSql => $@"
        INSERT INTO {ts.GroupEncryptionKeys}
        (
            {cgek.GroupShellId}, {cgek.DataCategory}, {cgek.WrappedDek}
        )
        VALUES
        (
            {pn.GroupShellId}, {pn.DataCategory}, {pn.WrappedDek}
        )
        RETURNING
            {cgek.GroupEncryptionKeyId}, {cgek.GroupEncryptionKeyUuid}, {cgek.GroupShellId},
            {cgek.DataCategory}, {cgek.WrappedDek}, {cgek.IsActive}, {cgek.InsertedOn}, {cgek.UpdatedOn}
        ;";

    /// <summary>SQL to select the single active key for a (GroupShellId, DataCategory) pair, if any.</summary>
    public static string GetActiveSql => $@"
        SELECT
            {cgek.GroupEncryptionKeyId}, {cgek.GroupEncryptionKeyUuid}, {cgek.GroupShellId},
            {cgek.DataCategory}, {cgek.WrappedDek}, {cgek.IsActive}, {cgek.InsertedOn}, {cgek.UpdatedOn}
        FROM
            {ts.GroupEncryptionKeys}
        WHERE
            {cgek.GroupShellId} = {pn.GroupShellId} AND {cgek.DataCategory} = {pn.DataCategory} AND {cgek.IsActive} = true
        LIMIT 1
        ;";

    #endregion

    /// <summary>Maps the current row of <paramref name="reader"/> to a <see cref="GroupEncryptionKey"/>.</summary>
    public static GroupEncryptionKey ToGroupEncryptionKey(this NpgsqlDataReader reader)
    {
        return new GroupEncryptionKey
        {
            GroupEncryptionKeyId = reader.GetInt32(os.GroupEncryptionKeyId),
            GroupEncryptionKeyUuid = reader.GetGuid(os.GroupEncryptionKeyUuid),
            GroupShellId = reader.GetInt32(os.GroupShellId),
            DataCategory = (EncryptionDataCategory)reader.GetInt32(os.DataCategory),
            WrappedDek = reader.GetString(os.WrappedDek),
            IsActive = reader.GetFieldValue<bool>(os.IsActive),
            InsertedOn = reader.GetFieldValue<DateTimeOffset>(os.InsertedOn),
            UpdatedOn = reader.GetFieldValue<DateTimeOffset?>(os.UpdatedOn)
        };
    }
}
