using Cassandra;
using Media.Database.Models;
#pragma warning disable CS8981
using coa = Media.Database.Repositories.Schemas.TablesCql.OtpAttemptsColumns;
using pn = Media.Database.Repositories.Schemas.ParameterNames;
using tc = Media.Database.Repositories.Schemas.TablesCql;
#pragma warning restore CS8981

namespace Media.Database.Repositories.Queries;

/// <summary>
/// CQL query text, and row mapping, for the otp_attempts table (MEDIA-40) -- Scylla only, no
/// Postgres counterpart or CDC feed: it is the system of record for its own short-lived rows.
/// </summary>
public static class QueryOtpAttempts
{
    /// <summary>CQL to select one nonce's counter.</summary>
    public static string GetSql => $@"
        SELECT
            {coa.NonceId},
            {coa.Attempts},
            {coa.IsUsed},
            {coa.IssuedOn}
        FROM {tc.OtpAttempts}
        WHERE {coa.NonceId} = {pn.NonceId}
        ;";

    /// <summary>
    /// CQL to write (or overwrite) a counter with a TTL, so it disappears when the window closes.
    /// The TTL is bound per write because the window is configuration, not a table default.
    /// </summary>
    public static string UpsertSql => $@"
        INSERT INTO {tc.OtpAttempts}
        (
            {coa.NonceId},
            {coa.Attempts},
            {coa.IsUsed},
            {coa.IssuedOn}
        )
        VALUES
        (
            {pn.NonceId},
            {pn.Attempts},
            {pn.IsUsed},
            {pn.IssuedOn}
        )
        USING TTL {pn.TimeToLiveSeconds}
        ;";

    /// <summary>Maps the current <paramref name="row"/> to an <see cref="OtpAttempt"/>.</summary>
    public static OtpAttempt ToOtpAttempt(this Row row)
    {
        return new OtpAttempt
        {
            NonceId = row.GetValue<string>(coa.NonceId),
            Attempts = row.GetValue<int>(coa.Attempts),
            IsUsed = row.GetValue<bool>(coa.IsUsed),
            IssuedOn = row.GetValue<DateTimeOffset>(coa.IssuedOn)
        };
    }
}
