using Media.Database.Helpers;
using Media.Database.Models;
using Npgsql;

#pragma warning disable CS8981
using cj = Media.Database.Repositories.Schemas.TablesSql.GroupJoinRequestsColumns;
using cjm = Media.Database.Repositories.Schemas.TablesSql.GroupJoinRequestMutesColumns;
using cm = Media.Database.Repositories.Schemas.TablesSql.MessagesColumns;
using cn = Media.Database.Repositories.Schemas.TablesSql.NotificationsColumns;
using cp = Media.Database.Repositories.Schemas.TablesSql.PersonsColumns;
using ccj = Media.Database.Repositories.Schemas.TablesCql.GroupJoinRequestsColumns;
using os = Media.Database.Repositories.Schemas.OrdinalsSql;
using pn = Media.Database.Repositories.Schemas.ParameterNames;
using tc = Media.Database.Repositories.Schemas.TablesCql;
using ts = Media.Database.Repositories.Schemas.TablesSql;
#pragma warning restore CS8981

namespace Media.Database.Repositories.Queries;

/// <summary>
/// SQL and CQL for requests to join a group (SCHEMA-35, MEDIA-8). Postgres owns every rule -- one
/// open (pending or ignored) request per person per group is its partial unique index -- and
/// Scylla holds a CDC-fed copy by id.
/// </summary>
public static class QueryGroupJoinRequests
{
    private static string Columns => $@"
            {cj.GroupJoinRequestId},
            {cj.GroupJoinRequestUuid},
            {cj.GroupId},
            {cj.PersonId},
            {cj.Status},
            {cj.AnsweredByPersonId},
            {cj.AnsweredOn},
            {cj.InsertedOn},
            {cj.UpdatedOn}";

    #region SQL Queries

    /// <summary>
    /// SQL to open a request, or do nothing when the person already has an open one for the group --
    /// pending, or ignored for good. Returns the new row, or no row when it was absorbed (read the
    /// open one with <see cref="GetOpenSql"/>).
    /// </summary>
    public static string AddSql => $@"
        INSERT INTO {ts.GroupJoinRequests} (
            {cj.GroupId},
            {cj.PersonId}
        ) VALUES (
            {pn.GroupId},
            {pn.PersonId}
        )
        ON CONFLICT ({cj.GroupId}, {cj.PersonId}) WHERE {cj.Status} IN (0, 3) DO NOTHING
        RETURNING{Columns}
        ;";

    /// <summary>SQL to read a person's open (pending or ignored) request to a group.</summary>
    public static string GetOpenSql => $@"
        SELECT{Columns}
        FROM {ts.GroupJoinRequests}
        WHERE
            {cj.GroupId} = {pn.GroupId}
            AND {cj.PersonId} = {pn.PersonId}
            AND {cj.Status} IN (0, 3)
        ;";

    /// <summary>SQL to read a request by its external identifier.</summary>
    public static string GetByUuidSql => $@"
        SELECT{Columns}
        FROM {ts.GroupJoinRequests}
        WHERE {cj.GroupJoinRequestUuid} = {pn.GroupJoinRequestUuid}
        ;";

    /// <summary>
    /// SQL to list a group's pending requests, oldest first, with each requester's name and email for
    /// the admins to recognise them by. A requester no longer active is left out.
    /// </summary>
    public static string ListPendingByGroupSql => $@"
        SELECT
            r.{cj.GroupJoinRequestUuid},
            p.{cp.PersonUuid},
            p.{cp.FirstName},
            p.{cp.LastName},
            p.{cp.EmailAddress},
            r.{cj.InsertedOn}
        FROM
            {ts.GroupJoinRequests} AS r
        JOIN
            {ts.Persons} AS p ON p.{cp.PersonId} = r.{cj.PersonId}
        WHERE
            r.{cj.GroupId} = {pn.GroupId}
            AND r.{cj.Status} = 0
            AND p.{cp.IsActive} = true
            -- Not those the admin reading muted (SCHEMA-38): theirs to the other admins only.
            AND NOT EXISTS (
                SELECT 1 FROM {ts.GroupJoinRequestMutes} AS m
                WHERE m.{cjm.GroupId} = r.{cj.GroupId}
                    AND m.{cjm.PersonId} = r.{cj.PersonId}
                    AND m.{cjm.MutedByPersonId} = {pn.MutedByPersonId}
            )
        ORDER BY
            r.{cj.InsertedOn} ASC,
            r.{cj.GroupJoinRequestId} ASC
        ;";

    /// <summary>
    /// SQL to answer a pending request -- accept, reject or ignore -- recording who and when. Only a
    /// pending request of the named group changes, so answering twice, or another group's request,
    /// changes nothing and returns no row.
    /// </summary>
    public static string AnswerSql => $@"
        UPDATE {ts.GroupJoinRequests} SET
            {cj.Status} = {pn.Status},
            {cj.AnsweredByPersonId} = {pn.AnsweredByPersonId},
            {cj.AnsweredOn} = {pn.Now},
            {cj.UpdatedOn} = {pn.Now}
        WHERE
            {cj.GroupJoinRequestUuid} = {pn.GroupJoinRequestUuid}
            AND {cj.GroupId} = {pn.GroupId}
            AND {cj.Status} = 0
        RETURNING{Columns}
        ;";

    /// <summary>
    /// SQL to close the open (new or seen) notifications of a request (DATABASE-68): each one made
    /// from the request.queued message that names it, marked acted -- someone has answered it.
    /// Returns how many were closed.
    /// </summary>
    public static string CloseNotificationsSql => $@"
        WITH closed AS (
            UPDATE {ts.Notifications} n SET
                {cn.Status} = {(int)NotificationStatus.Acted},
                {cn.UpdatedOn} = {pn.Now}
            FROM {ts.Messages} m
            WHERE m.{cm.MessageId} = n.{cn.MessageId}
                AND m.{cm.Kind} = '{AuditKinds.RequestQueued}'
                AND (m.{cm.Parameters} ->> '{AuditKinds.RequestUuidParameter}')::uuid = {pn.GroupJoinRequestUuid}
                AND n.{cn.Status} IN ({(int)NotificationStatus.New}, {(int)NotificationStatus.Seen})
            RETURNING 1
        )
        SELECT COUNT(*) FROM closed
        ;";

    #endregion

    #region CQL Queries

    /// <summary>CQL to insert/replace a request's Scylla copy.</summary>
    public static string UpsertCql => $@"
        INSERT INTO {tc.GroupJoinRequests}
        (
            {ccj.GroupJoinRequestId},
            {ccj.GroupJoinRequestUuid},
            {ccj.GroupId},
            {ccj.PersonId},
            {ccj.Status},
            {ccj.AnsweredByPersonId},
            {ccj.AnsweredOn},
            {ccj.InsertedOn},
            {ccj.UpdatedOn}
        )
        VALUES
        (
            {pn.GroupJoinRequestId},
            {pn.GroupJoinRequestUuid},
            {pn.GroupId},
            {pn.PersonId},
            {pn.Status},
            {pn.AnsweredByPersonId},
            {pn.AnsweredOn},
            {pn.InsertedOn},
            {pn.UpdatedOn}
        );";

    /// <summary>CQL to delete a request's Scylla copy.</summary>
    public static string DeleteCql => $@"
        DELETE FROM {tc.GroupJoinRequests} WHERE {ccj.GroupJoinRequestId} = {pn.GroupJoinRequestId};";

    /// <summary>
    /// SQL to mute one person's requests to a group for one admin (DATABASE-69): one row, however
    /// often it is asked for.
    /// </summary>
    public static string MuteSql => $@"
        INSERT INTO {ts.GroupJoinRequestMutes} (
            {cjm.GroupId},
            {cjm.PersonId},
            {cjm.MutedByPersonId}
        ) VALUES (
            {pn.GroupId},
            {pn.PersonId},
            {pn.MutedByPersonId}
        )
        ON CONFLICT ({cjm.GroupId}, {cjm.PersonId}, {cjm.MutedByPersonId}) DO NOTHING
        ;";

    /// <summary>
    /// SQL to close one admin's open notifications of a request (DATABASE-69): a mute ends their own
    /// being asked, not the other admins'. Returns how many were closed.
    /// </summary>
    public static string CloseNotificationsOfSql => $@"
        WITH closed AS (
            UPDATE {ts.Notifications} n SET
                {cn.Status} = {(int)NotificationStatus.Acted},
                {cn.UpdatedOn} = {pn.Now}
            FROM {ts.Messages} m
            WHERE m.{cm.MessageId} = n.{cn.MessageId}
                AND m.{cm.Kind} = '{AuditKinds.RequestQueued}'
                AND (m.{cm.Parameters} ->> '{AuditKinds.RequestUuidParameter}')::uuid = {pn.GroupJoinRequestUuid}
                AND n.{cn.RecipientPersonId} = {pn.MutedByPersonId}
                AND n.{cn.Status} IN ({(int)NotificationStatus.New}, {(int)NotificationStatus.Seen})
            RETURNING 1
        )
        SELECT COUNT(*) FROM closed
        ;";

    /// <summary>SQL for the admins who muted a person's requests to a group (DATABASE-69).</summary>
    public static string ListMutedBySql => $@"
        SELECT {cjm.MutedByPersonId}
        FROM {ts.GroupJoinRequestMutes}
        WHERE {cjm.GroupId} = {pn.GroupId}
            AND {cjm.PersonId} = {pn.PersonId}
        ;";

    #endregion

    /// <summary>Maps the current row of <paramref name="reader"/> to a <see cref="GroupJoinRequest"/>.</summary>
    public static GroupJoinRequest ToGroupJoinRequest(this NpgsqlDataReader reader)
    {
        return new GroupJoinRequest
        {
            GroupJoinRequestId = reader.GetInt32(os.GroupJoinRequestId),
            GroupJoinRequestUuid = reader.GetGuid(os.GroupJoinRequestUuid),
            GroupId = reader.GetInt32(os.GroupId),
            PersonId = reader.GetInt32(os.PersonId),
            Status = (GroupJoinRequestStatus)reader.GetInt32(os.Status),
            AnsweredByPersonId = reader.GetFieldValue<int?>(os.AnsweredByPersonId),
            AnsweredOn = reader.GetFieldValue<DateTimeOffset?>(os.AnsweredOn),
            InsertedOn = reader.GetFieldValue<DateTimeOffset>(os.InsertedOn),
            UpdatedOn = reader.GetFieldValue<DateTimeOffset?>(os.UpdatedOn)
        };
    }

    /// <summary>Maps the current row of <paramref name="reader"/> to a <see cref="PendingGroupJoinRequest"/>.</summary>
    public static PendingGroupJoinRequest ToPendingGroupJoinRequest(this NpgsqlDataReader reader)
    {
        return new PendingGroupJoinRequest
        {
            GroupJoinRequestUuid = reader.GetGuid(os.GroupJoinRequestUuid),
            PersonUuid = reader.GetGuid(os.PersonUuid),
            FirstName = reader.GetString(os.FirstName),
            LastName = reader.GetString(os.LastName),
            EmailAddress = reader.GetString(os.EmailAddress),
            InsertedOn = reader.GetFieldValue<DateTimeOffset>(os.InsertedOn)
        };
    }
}
