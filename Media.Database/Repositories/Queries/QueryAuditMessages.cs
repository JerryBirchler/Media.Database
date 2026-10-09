using Media.Database.Helpers;
using Media.Database.Models;
using Npgsql;

#pragma warning disable CS8981
using ca = Media.Database.Repositories.Schemas.TablesSql.AuditMessagesColumns;
using cm = Media.Database.Repositories.Schemas.TablesSql.MessagesColumns;
using os = Media.Database.Repositories.Schemas.OrdinalsSql;
using pn = Media.Database.Repositories.Schemas.ParameterNames;
using ts = Media.Database.Repositories.Schemas.TablesSql;
#pragma warning restore CS8981

namespace Media.Database.Repositories.Queries;

/// <summary>
/// SQL for the audit (DATABASE-63, MEDIA-53): a message and the audit entry that refers to it,
/// written as one statement, and a group's history read back newest first with the message that
/// says each entry. Postgres only for now; the Scylla copy comes with DATABASE-64.
/// </summary>
public static class QueryAuditMessages
{
    #region SQL Queries

    /// <summary>
    /// SQL to record what happened: inserts the catalogued message, then the audit entry that refers
    /// to it, in one statement, so neither is ever written without the other. Run inside the
    /// transaction of the change it records. Returns the new audit entry's id.
    /// </summary>
    public static string RecordSql => $@"
        WITH message AS (
            INSERT INTO {ts.Messages} ({cm.Kind}, {cm.Parameters})
            VALUES ({pn.Kind}, CAST({pn.Parameters} AS jsonb))
            RETURNING {cm.MessageId}
        )
        INSERT INTO {ts.AuditMessages} ({ca.MessageId}, {ca.GroupId}, {ca.SubjectPersonId}, {ca.ActorPersonId})
        SELECT {cm.MessageId}, {pn.GroupId}, {pn.SubjectPersonId}, {pn.ActorPersonId}
        FROM message
        RETURNING {ca.AuditMessageId}
        ;";

    /// <summary>
    /// SQL to read a page of a group's history, newest first, each entry with its message. Keyset
    /// paged on the entry's id: <c>@AuditMessageId</c> is the last id of the previous page, or null
    /// for the first.
    /// </summary>
    public static string ListByGroupSql => $@"
        SELECT
            a.{ca.AuditMessageId}, a.{ca.AuditMessageUuid}, a.{ca.GroupId}, a.{ca.SubjectPersonId},
            a.{ca.ActorPersonId}, a.{ca.NotificationId}, a.{ca.InsertedOn},
            m.{cm.Kind}, CAST(m.{cm.Parameters} AS text) AS {cm.Parameters}, m.{cm.Text}, m.{cm.Language}
        FROM {ts.AuditMessages} a
        JOIN {ts.Messages} m ON m.{cm.MessageId} = a.{ca.MessageId}
        WHERE a.{ca.GroupId} = {pn.GroupId}
            AND ({pn.AuditMessageId}::bigint IS NULL OR a.{ca.AuditMessageId} < {pn.AuditMessageId}::bigint)
        ORDER BY a.{ca.AuditMessageId} DESC
        LIMIT {pn.Limit}
        ;";

    #endregion

    /// <summary>Maps a row of <see cref="ListByGroupSql"/> to an <see cref="AuditMessage"/>.</summary>
    public static AuditMessage ToAuditMessage(this NpgsqlDataReader reader) => new()
    {
        AuditMessageId = reader.GetFieldValue<long>(os.AuditMessageId),
        AuditMessageUuid = reader.GetFieldValue<Guid>(os.AuditMessageUuid),
        Kind = NullableString(reader, os.Kind),
        Parameters = NullableString(reader, os.Parameters),
        Text = NullableString(reader, os.Text),
        Language = reader.GetFieldValue<string>(os.Language),
        GroupId = reader.GetFieldValue<int?>(os.GroupId),
        SubjectPersonId = reader.GetFieldValue<int?>(os.SubjectPersonId),
        ActorPersonId = reader.GetFieldValue<int?>(os.ActorPersonId),
        NotificationId = reader.GetFieldValue<long?>(os.NotificationId),
        InsertedOn = reader.GetFieldValue<DateTimeOffset>(os.InsertedOn)
    };

    /// <summary>A text column that may be null: Npgsql reads a null as an error unless asked first.</summary>
    private static string? NullableString(NpgsqlDataReader reader, string column)
    {
        var ordinal = reader.GetOrdinal(column);
        return reader.IsDBNull(ordinal) ? null : reader.GetString(ordinal);
    }
}
