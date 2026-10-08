using Media.Database.Helpers;
using Media.Database.Models;
using Npgsql;

#pragma warning disable CS8981
using cg = Media.Database.Repositories.Schemas.TablesSql.GroupsColumns;
using ci = Media.Database.Repositories.Schemas.TablesSql.GroupInvitesColumns;
using cp = Media.Database.Repositories.Schemas.TablesSql.PersonsColumns;
using cci = Media.Database.Repositories.Schemas.TablesCql.GroupInvitesColumns;
using os = Media.Database.Repositories.Schemas.OrdinalsSql;
using pn = Media.Database.Repositories.Schemas.ParameterNames;
using tc = Media.Database.Repositories.Schemas.TablesCql;
using ts = Media.Database.Repositories.Schemas.TablesSql;
#pragma warning restore CS8981

namespace Media.Database.Repositories.Queries;

/// <summary>
/// SQL and CQL for invitations to join a group (SCHEMA-35, MEDIA-8). Postgres owns every rule --
/// one pending invite per address per group is its partial unique index -- and Scylla holds a
/// CDC-fed copy by id.
///
/// A pending invite past its <c>ExpiresOn</c> is expired in every read and answer here, whether or
/// not the Worker's sweep (<see cref="ExpirePastDueSql"/>) has marked it yet, so nobody can accept
/// an invite in the gap between running out and being swept.
/// </summary>
public static class QueryGroupInvites
{
    private static string Columns => $@"
            {ci.GroupInviteId},
            {ci.GroupInviteUuid},
            {ci.GroupId},
            {ci.InvitedByPersonId},
            {ci.EmailAddress},
            {ci.LastName},
            {ci.Status},
            {ci.ExpiresOn},
            {ci.AcceptedByPersonId},
            {ci.AnsweredOn},
            {ci.InsertedOn},
            {ci.UpdatedOn}";

    #region SQL Queries

    /// <summary>
    /// SQL to invite an address to a group. Inviting an address that already has a pending invite
    /// to the group refreshes that invite -- new expiry, last name and inviter -- rather than
    /// queuing a second, which also revives one that ran out but has not been swept yet.
    /// </summary>
    public static string CreateSql => $@"
        INSERT INTO {ts.GroupInvites} (
            {ci.GroupId},
            {ci.InvitedByPersonId},
            {ci.EmailAddress},
            {ci.LastName},
            {ci.ExpiresOn},
            {ci.InsertedOn}
        ) VALUES (
            {pn.GroupId},
            {pn.InvitedByPersonId},
            {pn.EmailAddress},
            {pn.LastName},
            {pn.ExpiresOn},
            {pn.Now}
        )
        ON CONFLICT ({ci.GroupId}, {ci.EmailAddress}) WHERE {ci.Status} = 0 DO UPDATE SET
            {ci.InvitedByPersonId} = EXCLUDED.{ci.InvitedByPersonId},
            {ci.LastName} = EXCLUDED.{ci.LastName},
            {ci.ExpiresOn} = EXCLUDED.{ci.ExpiresOn},
            {ci.UpdatedOn} = {pn.Now}
        RETURNING{Columns}
        ;";

    /// <summary>SQL to read an invite by its external identifier.</summary>
    public static string GetByUuidSql => $@"
        SELECT{Columns}
        FROM {ts.GroupInvites}
        WHERE {ci.GroupInviteUuid} = {pn.GroupInviteUuid}
        ;";

    /// <summary>SQL to list a group's pending, unexpired invites, oldest first.</summary>
    public static string ListPendingByGroupSql => $@"
        SELECT{Columns}
        FROM {ts.GroupInvites}
        WHERE
            {ci.GroupId} = {pn.GroupId}
            AND {ci.Status} = 0
            AND {ci.ExpiresOn} > {pn.Now}
        ORDER BY
            {ci.InsertedOn} ASC,
            {ci.GroupInviteId} ASC
        ;";

    /// <summary>
    /// SQL to list the pending, unexpired invites addressed to an email address -- the recipient's
    /// bell -- with the group and the inviter's name. Invites to a group no longer active are left
    /// out.
    /// </summary>
    public static string ListPendingForEmailSql => $@"
        SELECT
            i.{ci.GroupInviteUuid},
            g.{cg.GroupUuid},
            g.{cg.Name} AS ""{os.GroupName}"",
            g.{cg.Title} AS ""{os.GroupTitle}"",
            p.{cp.FirstName} AS ""{os.InvitedByFirstName}"",
            p.{cp.LastName} AS ""{os.InvitedByLastName}"",
            i.{ci.ExpiresOn},
            i.{ci.InsertedOn}
        FROM
            {ts.GroupInvites} AS i
        JOIN
            {ts.Groups} AS g ON g.{cg.GroupId} = i.{ci.GroupId} AND g.{cg.IsActive} = true
        JOIN
            {ts.Persons} AS p ON p.{cp.PersonId} = i.{ci.InvitedByPersonId}
        WHERE
            i.{ci.EmailAddress} = {pn.EmailAddress}
            AND i.{ci.Status} = 0
            AND i.{ci.ExpiresOn} > {pn.Now}
        ORDER BY
            i.{ci.InsertedOn} ASC,
            i.{ci.GroupInviteId} ASC
        ;";

    /// <summary>
    /// SQL for the recipient to accept a pending, unexpired invite addressed to them. The address
    /// is part of the condition, so only the person it was sent to can accept it.
    /// </summary>
    public static string AcceptSql => $@"
        UPDATE {ts.GroupInvites} SET
            {ci.Status} = 1,
            {ci.AcceptedByPersonId} = {pn.AcceptedByPersonId},
            {ci.AnsweredOn} = {pn.Now},
            {ci.UpdatedOn} = {pn.Now}
        WHERE
            {ci.GroupInviteUuid} = {pn.GroupInviteUuid}
            AND {ci.EmailAddress} = {pn.EmailAddress}
            AND {ci.Status} = 0
            AND {ci.ExpiresOn} > {pn.Now}
        RETURNING{Columns}
        ;";

    /// <summary>SQL for the recipient to decline a pending, unexpired invite addressed to them.</summary>
    public static string DeclineSql => $@"
        UPDATE {ts.GroupInvites} SET
            {ci.Status} = 2,
            {ci.AnsweredOn} = {pn.Now},
            {ci.UpdatedOn} = {pn.Now}
        WHERE
            {ci.GroupInviteUuid} = {pn.GroupInviteUuid}
            AND {ci.EmailAddress} = {pn.EmailAddress}
            AND {ci.Status} = 0
            AND {ci.ExpiresOn} > {pn.Now}
        RETURNING{Columns}
        ;";

    /// <summary>SQL for the group's side to withdraw a pending invite of that group.</summary>
    public static string CancelSql => $@"
        UPDATE {ts.GroupInvites} SET
            {ci.Status} = 4,
            {ci.AnsweredOn} = {pn.Now},
            {ci.UpdatedOn} = {pn.Now}
        WHERE
            {ci.GroupInviteUuid} = {pn.GroupInviteUuid}
            AND {ci.GroupId} = {pn.GroupId}
            AND {ci.Status} = 0
        RETURNING{Columns}
        ;";

    /// <summary>SQL for the Worker sweep: marks every pending invite past its time expired.</summary>
    public static string ExpirePastDueSql => $@"
        UPDATE {ts.GroupInvites} SET
            {ci.Status} = 3,
            {ci.UpdatedOn} = {pn.Now}
        WHERE
            {ci.Status} = 0
            AND {ci.ExpiresOn} <= {pn.Now}
        ;";

    #endregion

    #region CQL Queries

    /// <summary>CQL to insert/replace an invite's Scylla copy.</summary>
    public static string UpsertCql => $@"
        INSERT INTO {tc.GroupInvites}
        (
            {cci.GroupInviteId},
            {cci.GroupInviteUuid},
            {cci.GroupId},
            {cci.InvitedByPersonId},
            {cci.EmailAddress},
            {cci.LastName},
            {cci.Status},
            {cci.ExpiresOn},
            {cci.AcceptedByPersonId},
            {cci.AnsweredOn},
            {cci.InsertedOn},
            {cci.UpdatedOn}
        )
        VALUES
        (
            {pn.GroupInviteId},
            {pn.GroupInviteUuid},
            {pn.GroupId},
            {pn.InvitedByPersonId},
            {pn.EmailAddress},
            {pn.LastName},
            {pn.Status},
            {pn.ExpiresOn},
            {pn.AcceptedByPersonId},
            {pn.AnsweredOn},
            {pn.InsertedOn},
            {pn.UpdatedOn}
        );";

    /// <summary>CQL to delete an invite's Scylla copy.</summary>
    public static string DeleteCql => $@"
        DELETE FROM {tc.GroupInvites} WHERE {cci.GroupInviteId} = {pn.GroupInviteId};";

    #endregion

    /// <summary>Maps the current row of <paramref name="reader"/> to a <see cref="GroupInvite"/>.</summary>
    public static GroupInvite ToGroupInvite(this NpgsqlDataReader reader)
    {
        return new GroupInvite
        {
            GroupInviteId = reader.GetInt32(os.GroupInviteId),
            GroupInviteUuid = reader.GetGuid(os.GroupInviteUuid),
            GroupId = reader.GetInt32(os.GroupId),
            InvitedByPersonId = reader.GetInt32(os.InvitedByPersonId),
            EmailAddress = reader.GetString(os.EmailAddress),
            LastName = reader.GetString(os.LastName),
            Status = (GroupInviteStatus)reader.GetInt32(os.Status),
            ExpiresOn = reader.GetFieldValue<DateTimeOffset>(os.ExpiresOn),
            AcceptedByPersonId = reader.GetFieldValue<int?>(os.AcceptedByPersonId),
            AnsweredOn = reader.GetFieldValue<DateTimeOffset?>(os.AnsweredOn),
            InsertedOn = reader.GetFieldValue<DateTimeOffset>(os.InsertedOn),
            UpdatedOn = reader.GetFieldValue<DateTimeOffset?>(os.UpdatedOn)
        };
    }

    /// <summary>Maps the current row of <paramref name="reader"/> to a <see cref="ReceivedGroupInvite"/>.</summary>
    public static ReceivedGroupInvite ToReceivedGroupInvite(this NpgsqlDataReader reader)
    {
        return new ReceivedGroupInvite
        {
            GroupInviteUuid = reader.GetGuid(os.GroupInviteUuid),
            GroupUuid = reader.GetGuid(os.GroupUuid),
            GroupName = reader.GetString(os.GroupName),
            GroupTitle = reader.GetString(os.GroupTitle),
            InvitedByFirstName = reader.GetString(os.InvitedByFirstName),
            InvitedByLastName = reader.GetString(os.InvitedByLastName),
            ExpiresOn = reader.GetFieldValue<DateTimeOffset>(os.ExpiresOn),
            InsertedOn = reader.GetFieldValue<DateTimeOffset>(os.InsertedOn)
        };
    }
}
