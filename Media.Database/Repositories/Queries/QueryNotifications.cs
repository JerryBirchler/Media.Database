using Media.Database.Helpers;
using Media.Database.Models;
using Npgsql;

#pragma warning disable CS8981
using ca = Media.Database.Repositories.Schemas.TablesSql.AuditMessagesColumns;
using cg = Media.Database.Repositories.Schemas.TablesSql.GroupsColumns;
using cm = Media.Database.Repositories.Schemas.TablesSql.MessagesColumns;
using cn = Media.Database.Repositories.Schemas.TablesSql.NotificationsColumns;
using cp = Media.Database.Repositories.Schemas.TablesSql.PersonsColumns;
using os = Media.Database.Repositories.Schemas.OrdinalsSql;
using pn = Media.Database.Repositories.Schemas.ParameterNames;
using ts = Media.Database.Repositories.Schemas.TablesSql;
#pragma warning restore CS8981

namespace Media.Database.Repositories.Queries;

/// <summary>
/// SQL for notifications (WORKER-29, API-188): made from an audit entry for the people its rule
/// names, read by their recipient with what the message is about, and moved on (seen, dismissed).
/// A notification copies nothing of its message: every read joins it.
/// </summary>
public static class QueryNotifications
{
    #region SQL Queries

    /// <summary>
    /// SQL to notify each recipient of a message once: one row per person, and none again for a
    /// person already notified of it (IX_Notifications_MessageId_RecipientPersonId) -- so an entry
    /// the Worker is shown twice notifies nobody twice. Returns how many were made.
    /// </summary>
    public static string CreateSql => $@"
        WITH made AS (
            INSERT INTO {ts.Notifications} ({cn.MessageId}, {cn.RecipientPersonId})
            SELECT {pn.MessageId}, recipient
            FROM unnest(CAST({pn.RecipientPersonIds} AS integer[])) AS recipient
            ON CONFLICT ({cn.MessageId}, {cn.RecipientPersonId}) DO NOTHING
            RETURNING 1
        )
        SELECT COUNT(*) FROM made
        ;";

    /// <summary>The name a person goes by: their spoken name or first name, then their last.</summary>
    private static string NameOf(string alias) =>
        $"TRIM(COALESCE(NULLIF({alias}.{cp.SpokenName}, ''), {alias}.{cp.FirstName}) || ' ' || {alias}.{cp.LastName})";

    /// <summary>
    /// SQL to read a page of a person's open notifications (new or seen), newest first, keyset paged
    /// on the notification's id: <c>@NotificationId</c> is the last id of the previous page, or null.
    /// Each with its message and what the message is about -- from the audit entry that first said
    /// it (the one that is not itself about a notification), when there is one.
    /// </summary>
    public static string ListOpenByRecipientSql => $@"
        SELECT
            n.{cn.NotificationId}, n.{cn.NotificationUuid}, n.{cn.Status}, n.{cn.IsPinned}, n.{cn.ExpiresOn}, n.{cn.InsertedOn},
            m.{cm.Kind}, CAST(m.{cm.Parameters} AS text) AS {cm.Parameters}, m.{cm.Text}, m.{cm.Language},
            g.{cg.Title} AS ""{os.GroupTitle}"",
            {NameOf("sp")} AS ""{os.SubjectName}"",
            {NameOf("ap")} AS ""{os.ActorName}""
        FROM {ts.Notifications} n
        JOIN {ts.Messages} m ON m.{cm.MessageId} = n.{cn.MessageId}
        LEFT JOIN LATERAL (
            SELECT a.{ca.GroupId}, a.{ca.SubjectPersonId}, a.{ca.ActorPersonId}
            FROM {ts.AuditMessages} a
            WHERE a.{ca.MessageId} = n.{cn.MessageId} AND a.{ca.NotificationId} IS NULL
            ORDER BY a.{ca.AuditMessageId}
            LIMIT 1
        ) origin ON true
        LEFT JOIN {ts.Groups} g ON g.{cg.GroupId} = origin.{ca.GroupId}
        LEFT JOIN {ts.Persons} sp ON sp.{cp.PersonId} = origin.{ca.SubjectPersonId}
        LEFT JOIN {ts.Persons} ap ON ap.{cp.PersonId} = origin.{ca.ActorPersonId}
        WHERE n.{cn.RecipientPersonId} = {pn.RecipientPersonId}
            AND n.{cn.Status} IN (0, 1)
            AND ({pn.NotificationId}::bigint IS NULL OR n.{cn.NotificationId} < {pn.NotificationId}::bigint)
        ORDER BY n.{cn.NotificationId} DESC
        LIMIT {pn.Limit}
        ;";

    /// <summary>
    /// SQL to move one of the recipient's notifications on: to seen only from new, to dismissed from
    /// new or seen. Someone else's, or one already past it, is not changed -- no row comes back.
    /// Returns the notification's id, for the audit entry recording the change.
    /// </summary>
    public static string SetStatusSql => $@"
        UPDATE {ts.Notifications} SET
            {cn.Status} = {pn.Status},
            {cn.UpdatedOn} = {pn.UpdatedOn}
        WHERE {cn.NotificationUuid} = {pn.NotificationUuid}
            AND {cn.RecipientPersonId} = {pn.RecipientPersonId}
            AND (({pn.Status} = 1 AND {cn.Status} = 0) OR ({pn.Status} = 2 AND {cn.Status} IN (0, 1)))
        RETURNING {cn.NotificationId}
        ;";

    #endregion

    /// <summary>Maps a row of <see cref="ListOpenByRecipientSql"/> to a <see cref="NotificationView"/>.</summary>
    public static NotificationView ToNotificationView(this NpgsqlDataReader reader) => new()
    {
        NotificationId = reader.GetFieldValue<long>(os.NotificationId),
        NotificationUuid = reader.GetFieldValue<Guid>(os.NotificationUuid),
        Status = (NotificationStatus)reader.GetFieldValue<int>(os.Status),
        IsPinned = reader.GetFieldValue<bool>(os.IsPinned),
        ExpiresOn = reader.GetFieldValue<DateTimeOffset?>(os.ExpiresOn),
        InsertedOn = reader.GetFieldValue<DateTimeOffset>(os.InsertedOn),
        Kind = NullableString(reader, os.Kind),
        Parameters = NullableString(reader, os.Parameters),
        Text = NullableString(reader, os.Text),
        Language = reader.GetFieldValue<string>(os.Language),
        GroupTitle = NullableString(reader, os.GroupTitle),
        SubjectName = NullableString(reader, os.SubjectName),
        ActorName = NullableString(reader, os.ActorName)
    };

    /// <summary>A text column that may be null: Npgsql reads a null as an error unless asked first.</summary>
    private static string? NullableString(NpgsqlDataReader reader, string column)
    {
        var ordinal = reader.GetOrdinal(column);
        return reader.IsDBNull(ordinal) ? null : reader.GetString(ordinal);
    }
}
