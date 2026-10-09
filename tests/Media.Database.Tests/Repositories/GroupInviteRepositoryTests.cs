#nullable enable
using Media.Common.Transactions;
using System.Threading;
using AutoFixture;
using Media.Common.Archetypes;
using Media.Database.Models;
using Media.Database.Repositories;
using Media.Database.Repositories.Queries;
using Media.Database.Tests.TestHelpers;
using Microsoft.Extensions.Logging;
using Moq;
using Npgsql;
using NUnit.Framework;
using Shouldly;
using System;
using System.Linq;
using System.Threading.Tasks;
#pragma warning disable CS8981
using pn = Media.Database.Repositories.Schemas.ParameterNames;
#pragma warning restore CS8981

namespace Media.Database.Tests.Repositories;

[TestFixture]
public class GroupInviteRepositoryTests
{
    private Mock<ISqlQueryExecutor> _sqlExecutorMock = null!;
    private Mock<IAuditMessageRepository> _auditMock = null!;
    private Mock<IUnitOfWork> _unitOfWorkMock = null!;
    private IFixture _fixture = null!;

    /// <summary>What each change in these tests is recorded as (DATABASE-66).</summary>
    private static readonly AuditEntry Audit = new("invite.queued", SubjectPersonId: null, ActorPersonId: 7);

    [SetUp]
    public void Setup()
    {
        _fixture = AutoMoqFixture.Create();
        _sqlExecutorMock = _fixture.Freeze<Mock<ISqlQueryExecutor>>();
        _auditMock = new Mock<IAuditMessageRepository>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
    }

    private GroupInviteRepository CreateRepository() => new(
        _sqlExecutorMock.Object,
        _auditMock.Object,
        () => _unitOfWorkMock.Object,
        Mock.Of<ILogger<GroupInviteRepository>>());

    /// <summary>A statement run inside the change's transaction (DATABASE-66), answering <paramref name="result"/>.</summary>
    private void CaptureInTransaction(string sql, GroupInvite? result)
    {
        _sqlExecutorMock
            .Setup(e => e.QuerySingleAsync(_unitOfWorkMock.Object, sql, It.IsAny<Action<NpgsqlParameterCollection>>(), It.IsAny<Func<NpgsqlDataReader, GroupInvite>>()))
            .ReturnsAsync(result);
    }

    /// <summary>Whether the person is already a member, as the accept's transaction asks.</summary>
    private void AlreadyMember(bool member)
    {
        _sqlExecutorMock
            .Setup(e => e.QuerySingleValueAsync(_unitOfWorkMock.Object, QueryGroupsPersons.CountActiveMembershipSql, It.IsAny<Action<NpgsqlParameterCollection>>(), It.IsAny<Func<NpgsqlDataReader, long>>()))
            .ReturnsAsync(member ? 1L : 0L);
    }

    private void CaptureSingle(string sql, GroupInvite? result)
    {
        _sqlExecutorMock
            .Setup(e => e.QuerySingleAsync(sql, It.IsAny<Action<NpgsqlParameterCollection>>(), It.IsAny<Func<NpgsqlDataReader, GroupInvite>>()))
            .ReturnsAsync(result);
    }

    /// <summary>The parameter setup the repository passed with its last call for <paramref name="sql"/>.</summary>
    private Action<NpgsqlParameterCollection> Captured(string sql) =>
        (Action<NpgsqlParameterCollection>)_sqlExecutorMock.Invocations
            .Last(i => i.Arguments.Contains(sql))
            .Arguments.OfType<Action<NpgsqlParameterCollection>>().Single();

    private static NpgsqlParameterCollection Apply(Action<NpgsqlParameterCollection> configure)
    {
        var command = new NpgsqlCommand();
        configure(command.Parameters);
        return command.Parameters;
    }

    [Test]
    public void GroupInviteRepository_Should_Implement_IGroupInviteRepository()
    {
        CreateRepository().ShouldBeAssignableTo<IGroupInviteRepository>();
    }

    [Test]
    public async Task CreateAsync_Should_ExpireOneLifetimeFromNow()
    {
        var invite = _fixture.Create<GroupInvite>();
        var emailAddress = _fixture.Create<EmailAddress>();
        var lastName = _fixture.Create<PersonName>();
        var lifetime = TimeSpan.FromDays(14);
        CaptureInTransaction(QueryGroupInvites.CreateSql, invite);

        var before = DateTimeOffset.UtcNow;
        var result = await CreateRepository().CreateAsync(invite.GroupId, invite.InvitedByPersonId, emailAddress, lastName, lifetime, Audit);

        result.ShouldBe(invite);
        var parameters = Apply(Captured(QueryGroupInvites.CreateSql));
        parameters[pn.GroupId].Value.ShouldBe(invite.GroupId);
        parameters[pn.InvitedByPersonId].Value.ShouldBe(invite.InvitedByPersonId);
        parameters[pn.EmailAddress].Value.ShouldBe(emailAddress.ToString());
        parameters[pn.LastName].Value.ShouldBe(lastName.ToString());
        var now = (DateTimeOffset)parameters[pn.Now].Value!;
        now.ShouldBeGreaterThanOrEqualTo(before);
        parameters[pn.ExpiresOn].Value.ShouldBe(now + lifetime);
    }

    [TestCase(0)]
    [TestCase(-1)]
    public async Task CreateAsync_Should_Throw_When_TheLifetimeIsNotPositive(int days)
    {
        await Should.ThrowAsync<ArgumentOutOfRangeException>(() => CreateRepository().CreateAsync(
            _fixture.Create<int>(), _fixture.Create<int>(), _fixture.Create<EmailAddress>(), _fixture.Create<PersonName>(), TimeSpan.FromDays(days), Audit));

        _sqlExecutorMock.VerifyNoOtherCalls();
    }

    [Test]
    public async Task CreateAsync_Should_Throw_When_NoRowComesBack()
    {
        CaptureInTransaction(QueryGroupInvites.CreateSql, null);

        await Should.ThrowAsync<InvalidOperationException>(() => CreateRepository().CreateAsync(
            _fixture.Create<int>(), _fixture.Create<int>(), _fixture.Create<EmailAddress>(), _fixture.Create<PersonName>(), TimeSpan.FromDays(1), Audit));
    }

    [Test]
    public void CreateSql_Should_RefreshThePendingInviteRatherThanQueueASecond()
    {
        var sql = QueryGroupInvites.CreateSql;

        sql.ShouldContain(@"ON CONFLICT (""GroupId"", ""EmailAddress"") WHERE ""Status"" = 0 DO UPDATE SET");
        sql.ShouldContain(@"""ExpiresOn"" = EXCLUDED.""ExpiresOn""");
    }

    [Test]
    public async Task GetByUuidAsync_Should_ReturnTheInvite()
    {
        var invite = _fixture.Create<GroupInvite>();
        CaptureSingle(QueryGroupInvites.GetByUuidSql, invite);

        var result = await CreateRepository().GetByUuidAsync(invite.GroupInviteUuid);

        result.ShouldBe(invite);
        Apply(Captured(QueryGroupInvites.GetByUuidSql))[pn.GroupInviteUuid].Value.ShouldBe(invite.GroupInviteUuid);
    }

    [Test]
    public async Task ListPendingByGroupAsync_Should_ReturnTheGroupsPendingInvites()
    {
        var invites = _fixture.CreateMany<GroupInvite>(2).ToList();
        _sqlExecutorMock
            .Setup(e => e.QueryManyAsync(QueryGroupInvites.ListPendingByGroupSql, It.IsAny<Action<NpgsqlParameterCollection>>(), It.IsAny<Func<NpgsqlDataReader, GroupInvite>>()))
            .ReturnsAsync(invites);
        var groupId = _fixture.Create<int>();

        var result = await CreateRepository().ListPendingByGroupAsync(groupId);

        result.ShouldBe(invites);
        var parameters = Apply(Captured(QueryGroupInvites.ListPendingByGroupSql));
        parameters[pn.GroupId].Value.ShouldBe(groupId);
        parameters.Contains(pn.Now).ShouldBeTrue();
    }

    [Test]
    public async Task ListPendingForEmailAsync_Should_ReturnTheRecipientsInvites()
    {
        var invites = _fixture.CreateMany<ReceivedGroupInvite>(2).ToList();
        var emailAddress = _fixture.Create<EmailAddress>();
        _sqlExecutorMock
            .Setup(e => e.QueryManyAsync(QueryGroupInvites.ListPendingForEmailSql, It.IsAny<Action<NpgsqlParameterCollection>>(), It.IsAny<Func<NpgsqlDataReader, ReceivedGroupInvite>>()))
            .ReturnsAsync(invites);

        var result = await CreateRepository().ListPendingForEmailAsync(emailAddress);

        result.ShouldBe(invites);
        Apply(Captured(QueryGroupInvites.ListPendingForEmailSql))[pn.EmailAddress].Value.ShouldBe(emailAddress.ToString());
    }

    [Test]
    public void PendingReads_Should_TreatAnInvitePastItsTimeAsExpired()
    {
        QueryGroupInvites.ListPendingByGroupSql.ShouldContain(@"""ExpiresOn"" > @Now");
        QueryGroupInvites.ListPendingForEmailSql.ShouldContain(@"""ExpiresOn"" > @Now");
        QueryGroupInvites.AcceptSql.ShouldContain(@"""ExpiresOn"" > @Now");
        QueryGroupInvites.DeclineSql.ShouldContain(@"""ExpiresOn"" > @Now");
    }

    [Test]
    public async Task AcceptAsync_Should_AcceptOnlyAsTheAddressItWasSentTo()
    {
        var accepted = _fixture.Create<GroupInvite>() with { Status = GroupInviteStatus.Accepted };
        var emailAddress = _fixture.Create<EmailAddress>();
        CaptureInTransaction(QueryGroupInvites.AcceptSql, accepted);

        var result = await CreateRepository().AcceptAsync(accepted.GroupInviteUuid, emailAddress, accepted.AcceptedByPersonId!.Value, Audit);

        result.ShouldBe(accepted);
        var parameters = Apply(Captured(QueryGroupInvites.AcceptSql));
        parameters[pn.GroupInviteUuid].Value.ShouldBe(accepted.GroupInviteUuid);
        parameters[pn.EmailAddress].Value.ShouldBe(emailAddress.ToString());
        parameters[pn.AcceptedByPersonId].Value.ShouldBe(accepted.AcceptedByPersonId);
        QueryGroupInvites.AcceptSql.ShouldContain(@"""EmailAddress"" = @EmailAddress");
    }

    [Test]
    public async Task AcceptAsync_Should_ReturnNull_When_NoPendingInviteMatches()
    {
        CaptureInTransaction(QueryGroupInvites.AcceptSql, null);

        var result = await CreateRepository().AcceptAsync(Guid.NewGuid(), _fixture.Create<EmailAddress>(), _fixture.Create<int>(), Audit);

        result.ShouldBeNull();
    }

    [Test]
    public async Task DeclineAsync_Should_ReturnTheDeclinedInvite()
    {
        var declined = _fixture.Create<GroupInvite>() with { Status = GroupInviteStatus.Declined };
        var emailAddress = _fixture.Create<EmailAddress>();
        CaptureInTransaction(QueryGroupInvites.DeclineSql, declined);

        var result = await CreateRepository().DeclineAsync(declined.GroupInviteUuid, emailAddress, Audit);

        result.ShouldBe(declined);
        Apply(Captured(QueryGroupInvites.DeclineSql))[pn.EmailAddress].Value.ShouldBe(emailAddress.ToString());
    }

    [Test]
    public async Task CreateAsync_Should_RecordTheInvite_WithItsUuid_InTheSameTransaction()
    {
        var invite = _fixture.Create<GroupInvite>();
        CaptureInTransaction(QueryGroupInvites.CreateSql, invite);

        await CreateRepository().CreateAsync(invite.GroupId, 7, _fixture.Create<EmailAddress>(), _fixture.Create<PersonName>(), TimeSpan.FromDays(14), Audit);

        _auditMock.Verify(a => a.RecordAsync(_unitOfWorkMock.Object, invite.GroupId,
            It.Is<AuditEntry>(e => e.Kind == "invite.queued" && e.Parameters!.Contains(invite.GroupInviteUuid.ToString()))), Times.Once);
        _unitOfWorkMock.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task AcceptAsync_Should_MakeThemAMember_AndRecordIt_Together()
    {
        var accepted = _fixture.Create<GroupInvite>() with { Status = GroupInviteStatus.Accepted };
        CaptureInTransaction(QueryGroupInvites.AcceptSql, accepted);
        AlreadyMember(false);

        await CreateRepository().AcceptAsync(accepted.GroupInviteUuid, _fixture.Create<EmailAddress>(), 9, Audit);

        _sqlExecutorMock.Verify(e => e.ExecuteAsync(_unitOfWorkMock.Object, QueryGroupsPersons.UpsertSql, It.IsAny<Action<NpgsqlParameterCollection>>()), Times.Once);
        _auditMock.Verify(a => a.RecordAsync(_unitOfWorkMock.Object, accepted.GroupId, It.IsAny<AuditEntry>()), Times.Once);
        _unitOfWorkMock.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task AcceptAsync_Should_LeaveAMemberAsTheyAre_NeverDemotingAnAdmin()
    {
        var accepted = _fixture.Create<GroupInvite>() with { Status = GroupInviteStatus.Accepted };
        CaptureInTransaction(QueryGroupInvites.AcceptSql, accepted);
        AlreadyMember(true);

        await CreateRepository().AcceptAsync(accepted.GroupInviteUuid, _fixture.Create<EmailAddress>(), 9, Audit);

        _sqlExecutorMock.Verify(e => e.ExecuteAsync(It.IsAny<IUnitOfWork>(), QueryGroupsPersons.UpsertSql, It.IsAny<Action<NpgsqlParameterCollection>>()), Times.Never);
    }

    [Test]
    public async Task AcceptAsync_Should_RecordNothing_When_NoPendingInviteMatches()
    {
        CaptureInTransaction(QueryGroupInvites.AcceptSql, null);

        await CreateRepository().AcceptAsync(Guid.NewGuid(), _fixture.Create<EmailAddress>(), 9, Audit);

        _auditMock.Verify(a => a.RecordAsync(It.IsAny<IUnitOfWork>(), It.IsAny<int?>(), It.IsAny<AuditEntry>()), Times.Never);
        _sqlExecutorMock.Verify(e => e.ExecuteAsync(It.IsAny<IUnitOfWork>(), QueryGroupsPersons.UpsertSql, It.IsAny<Action<NpgsqlParameterCollection>>()), Times.Never);
    }

    [Test]
    public async Task DeclineAsync_Should_RecordIt_InTheSameTransaction()
    {
        var declined = _fixture.Create<GroupInvite>() with { Status = GroupInviteStatus.Declined };
        CaptureInTransaction(QueryGroupInvites.DeclineSql, declined);

        await CreateRepository().DeclineAsync(declined.GroupInviteUuid, _fixture.Create<EmailAddress>(), Audit);

        _auditMock.Verify(a => a.RecordAsync(_unitOfWorkMock.Object, declined.GroupId, It.IsAny<AuditEntry>()), Times.Once);
    }

    [Test]
    public async Task CancelAsync_Should_RecordIt_AndCloseTheInviteesNotification_InOneTransaction()
    {
        var cancelled = _fixture.Create<GroupInvite>() with { Status = GroupInviteStatus.Cancelled };
        CaptureInTransaction(QueryGroupInvites.CancelSql, cancelled);
        var audit = new AuditEntry("invite.cancelled", SubjectPersonId: null, ActorPersonId: 7);

        await CreateRepository().CancelAsync(cancelled.GroupId, cancelled.GroupInviteUuid, audit);

        _auditMock.Verify(a => a.RecordAsync(_unitOfWorkMock.Object, cancelled.GroupId,
            It.Is<AuditEntry>(e => e.Kind == "invite.cancelled" && e.Parameters!.Contains(cancelled.GroupInviteUuid.ToString()))), Times.Once);
        _sqlExecutorMock.Verify(e => e.QuerySingleValueAsync(_unitOfWorkMock.Object, QueryGroupInvites.CloseNotificationsSql,
            It.IsAny<Action<NpgsqlParameterCollection>>(), It.IsAny<Func<NpgsqlDataReader, long>>()), Times.Once);
    }

    [Test]
    public async Task CancelAsync_Should_RecordNothing_When_NothingPendingMatches()
    {
        CaptureInTransaction(QueryGroupInvites.CancelSql, null);

        var result = await CreateRepository().CancelAsync(1, Guid.NewGuid(), Audit);

        result.ShouldBeNull();
        _auditMock.Verify(a => a.RecordAsync(It.IsAny<IUnitOfWork>(), It.IsAny<int?>(), It.IsAny<AuditEntry>()), Times.Never);
    }

    [Test]
    public void CloseNotificationsSql_Should_CloseOnlyOpenNotificationsOfThatInvite()
    {
        var sql = QueryGroupInvites.CloseNotificationsSql;

        sql.ShouldContain("'invite.queued'");
        sql.ShouldContain("->> 'inviteUuid')::uuid = @GroupInviteUuid");
        sql.ShouldContain(@"""Status"" IN (0, 1)");
    }

    [Test]
    public async Task CancelAsync_Should_CancelOnlyAnInviteOfTheNamedGroup()
    {
        var cancelled = _fixture.Create<GroupInvite>() with { Status = GroupInviteStatus.Cancelled };
        CaptureInTransaction(QueryGroupInvites.CancelSql, cancelled);

        var result = await CreateRepository().CancelAsync(cancelled.GroupId, cancelled.GroupInviteUuid, Audit);

        result.ShouldBe(cancelled);
        var parameters = Apply(Captured(QueryGroupInvites.CancelSql));
        parameters[pn.GroupId].Value.ShouldBe(cancelled.GroupId);
        parameters[pn.GroupInviteUuid].Value.ShouldBe(cancelled.GroupInviteUuid);
        QueryGroupInvites.CancelSql.ShouldContain(@"""GroupId"" = @GroupId");
    }

    [Test]
    public async Task ExpirePastDueAsync_Should_ReturnHowManyExpired()
    {
        var count = _fixture.Create<int>();
        _sqlExecutorMock
            .Setup(e => e.ExecuteAsync(QueryGroupInvites.ExpirePastDueSql, It.IsAny<Action<NpgsqlParameterCollection>>()))
            .ReturnsAsync(count);

        var result = await CreateRepository().ExpirePastDueAsync();

        result.ShouldBe(count);
    }

    [Test]
    public async Task ExpirePastDueAsync_Should_Rethrow_When_ExecutorThrows()
    {
        _sqlExecutorMock
            .Setup(e => e.ExecuteAsync(QueryGroupInvites.ExpirePastDueSql, It.IsAny<Action<NpgsqlParameterCollection>>()))
            .ThrowsAsync(new InvalidOperationException("boom"));

        await Should.ThrowAsync<InvalidOperationException>(() => CreateRepository().ExpirePastDueAsync());
    }
}
