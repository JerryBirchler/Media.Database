#nullable enable
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
    private IFixture _fixture = null!;

    [SetUp]
    public void Setup()
    {
        _fixture = AutoMoqFixture.Create();
        _sqlExecutorMock = _fixture.Freeze<Mock<ISqlQueryExecutor>>();
    }

    private GroupInviteRepository CreateRepository() => new(
        _sqlExecutorMock.Object,
        Mock.Of<ILogger<GroupInviteRepository>>());

    private void CaptureSingle(string sql, GroupInvite? result)
    {
        _sqlExecutorMock
            .Setup(e => e.QuerySingleAsync(sql, It.IsAny<Action<NpgsqlParameterCollection>>(), It.IsAny<Func<NpgsqlDataReader, GroupInvite>>()))
            .ReturnsAsync(result);
    }

    /// <summary>The parameter setup the repository passed with its last call for <paramref name="sql"/>.</summary>
    private Action<NpgsqlParameterCollection> Captured(string sql) =>
        (Action<NpgsqlParameterCollection>)_sqlExecutorMock.Invocations
            .Last(i => i.Arguments.Count == 3 && Equals(i.Arguments[0], sql))
            .Arguments[1];

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
        CaptureSingle(QueryGroupInvites.CreateSql, invite);

        var before = DateTimeOffset.UtcNow;
        var result = await CreateRepository().CreateAsync(invite.GroupId, invite.InvitedByPersonId, emailAddress, lastName, lifetime);

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
            _fixture.Create<int>(), _fixture.Create<int>(), _fixture.Create<EmailAddress>(), _fixture.Create<PersonName>(), TimeSpan.FromDays(days)));

        _sqlExecutorMock.VerifyNoOtherCalls();
    }

    [Test]
    public async Task CreateAsync_Should_Throw_When_NoRowComesBack()
    {
        CaptureSingle(QueryGroupInvites.CreateSql, null);

        await Should.ThrowAsync<InvalidOperationException>(() => CreateRepository().CreateAsync(
            _fixture.Create<int>(), _fixture.Create<int>(), _fixture.Create<EmailAddress>(), _fixture.Create<PersonName>(), TimeSpan.FromDays(1)));
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
        CaptureSingle(QueryGroupInvites.AcceptSql, accepted);

        var result = await CreateRepository().AcceptAsync(accepted.GroupInviteUuid, emailAddress, accepted.AcceptedByPersonId!.Value);

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
        CaptureSingle(QueryGroupInvites.AcceptSql, null);

        var result = await CreateRepository().AcceptAsync(Guid.NewGuid(), _fixture.Create<EmailAddress>(), _fixture.Create<int>());

        result.ShouldBeNull();
    }

    [Test]
    public async Task DeclineAsync_Should_ReturnTheDeclinedInvite()
    {
        var declined = _fixture.Create<GroupInvite>() with { Status = GroupInviteStatus.Declined };
        var emailAddress = _fixture.Create<EmailAddress>();
        CaptureSingle(QueryGroupInvites.DeclineSql, declined);

        var result = await CreateRepository().DeclineAsync(declined.GroupInviteUuid, emailAddress);

        result.ShouldBe(declined);
        Apply(Captured(QueryGroupInvites.DeclineSql))[pn.EmailAddress].Value.ShouldBe(emailAddress.ToString());
    }

    [Test]
    public async Task CancelAsync_Should_CancelOnlyAnInviteOfTheNamedGroup()
    {
        var cancelled = _fixture.Create<GroupInvite>() with { Status = GroupInviteStatus.Cancelled };
        CaptureSingle(QueryGroupInvites.CancelSql, cancelled);

        var result = await CreateRepository().CancelAsync(cancelled.GroupId, cancelled.GroupInviteUuid);

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
