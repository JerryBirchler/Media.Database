using Media.Database.Helpers;
using Media.Database.Models;
using Npgsql;

#pragma warning disable CS8981
using csk = Media.Database.Repositories.Schemas.TablesSql.SourceMachineKeysColumns;
using os = Media.Database.Repositories.Schemas.OrdinalsSql;
using pn = Media.Database.Repositories.Schemas.ParameterNames;
using ts = Media.Database.Repositories.Schemas.TablesSql;
#pragma warning restore CS8981

namespace Media.Database.Repositories.Queries;

/// <summary>
/// SQL query text, and reader mapping extensions, for the public keys a device authenticates with.
/// Only public halves are ever stored or selected here.
/// </summary>
public static class QuerySourceMachineKeys
{
    private static string AllColumns => $@"
            {csk.SourceMachineKeyId}, {csk.SourceMachineKeyUuid}, {csk.SourceMachineId},
            {csk.KeyPurpose}, {csk.Algorithm}, {csk.PublicKey}, {csk.IsActive},
            {csk.RevokedOn}, {csk.InsertedOn}, {csk.UpdatedOn}";

    #region SQL Queries

    /// <summary>SQL to enroll a new public key for a device.</summary>
    public static string EnrollSql => $@"
        INSERT INTO {ts.SourceMachineKeys} (
            {csk.SourceMachineId}, {csk.KeyPurpose}, {csk.Algorithm}, {csk.PublicKey}
        )
        VALUES (
            {pn.SourceMachineId}, {pn.KeyPurpose}, {pn.Algorithm}, {pn.PublicKey}
        )
        RETURNING{AllColumns}
        ;";

    /// <summary>
    /// SQL to select every active key for a device, both purposes, newest first -- more than one
    /// active operational key is legal during a rotation window.
    /// </summary>
    public static string GetActiveBySourceMachineIdSql => $@"
        SELECT{AllColumns}
        FROM
            {ts.SourceMachineKeys}
        WHERE
            {csk.SourceMachineId} = {pn.SourceMachineId}
            AND {csk.IsActive} = true
        ORDER BY
            {csk.SourceMachineKeyId} DESC
        ;";

    /// <summary>
    /// SQL to resolve a presented public key to its active enrollment. Returns nothing for a
    /// revoked key, which is what makes revocation take effect.
    /// </summary>
    public static string GetActiveByPublicKeySql => $@"
        SELECT{AllColumns}
        FROM
            {ts.SourceMachineKeys}
        WHERE
            {csk.PublicKey} = {pn.PublicKey}
            AND {csk.IsActive} = true
        LIMIT 1
        ;";

    /// <summary>
    /// SQL to revoke a key -- a no-op, returning nothing, if it is already revoked or does not
    /// exist. Same "WHERE ... = true" idempotence pattern as
    /// <c>QueryGroupShell.PromoteIfUnpromotedSql</c>, and the row is kept rather than deleted so it
    /// stays possible to tell whether a key was trusted at a given time.
    /// </summary>
    public static string RevokeIfActiveSql => $@"
        UPDATE {ts.SourceMachineKeys} SET
            {csk.IsActive} = false,
            {csk.RevokedOn} = {pn.RevokedOn},
            {csk.UpdatedOn} = {pn.UpdatedOn}
        WHERE
            {csk.SourceMachineKeyUuid} = {pn.SourceMachineKeyUuid}
            AND {csk.IsActive} = true
        RETURNING{AllColumns}
        ;";

    #endregion

    /// <summary>Maps the current row of <paramref name="reader"/> to a <see cref="SourceMachineKey"/>.</summary>
    public static SourceMachineKey ToSourceMachineKey(this NpgsqlDataReader reader)
    {
        return new SourceMachineKey
        {
            SourceMachineKeyId = reader.GetInt32(os.SourceMachineKeyId),
            SourceMachineKeyUuid = reader.GetFieldValue<Guid>(os.SourceMachineKeyUuid),
            SourceMachineId = reader.GetInt32(os.SourceMachineId),
            KeyPurpose = (SourceMachineKeyPurpose)reader.GetInt32(os.KeyPurpose),
            Algorithm = (SourceMachineKeyAlgorithm)reader.GetInt32(os.Algorithm),
            PublicKey = reader.GetFieldValue<string>(os.PublicKey),
            IsActive = reader.GetFieldValue<bool>(os.IsActive),
            RevokedOn = reader.GetFieldValue<DateTimeOffset?>(os.RevokedOn),
            InsertedOn = reader.GetFieldValue<DateTimeOffset>(os.InsertedOn),
            UpdatedOn = reader.GetFieldValue<DateTimeOffset?>(os.UpdatedOn)
        };
    }
}
