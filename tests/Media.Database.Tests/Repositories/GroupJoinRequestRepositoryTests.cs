#nullable enable
using AutoFixture;
using Media.Common.Transactions;
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
public class GroupJoinRequestRepositoryTests
{
    private Mock<ISqlQueryExecutor> _sqlExecutorMock = null!;
    private Mock<IAuditMessageRepository> _auditMock = null!;
    private Mock<IUnitOfWork> _unitOfWorkMock = null!;
    private IFixture _fixture = null!;

    /// <summary>What an answer is recorded as (DATABASE-68); who asked is filled in from the request.</summary>
    private static readonly AuditEntry Answer = new("request.declined", SubjectPersonId: null, ActorPersonId: 7);

    /// <summary>What a new request is recorded as (DATABASE-67).</summary>
    private static readonly AuditEntry Audit = new("request.queued", SubjectPersonId: 5, ActorPersonId: 5);

    [SetUp]
    public void Setup()
    {
        _fixture = AutoMoqFixture.Create();
        _sqlExecutorMock = _fixture.Freeze<Mock<ISqlQueryExecutor>>();
        _auditMock = new Mock<IAuditMessageRepository>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
    }

    private GroupJoinRequestRepository CreateRepository() => new(
        _sqlExecutorMock.Object,
        _auditMock.Object,
        () => _unitOfWorkMock.Object,
        Mock.Of<ILogger<GroupJoinRequestRepository>>());

    /// <summary>A submission's statement, run inside its transaction (DATABASE-67), answering each result in turn.</summary>
    private void SetupSingle(string sql, params GroupJoinRequest?[] results)
    {
        var sequence = _sqlExecutorMock.SetupSequence(e => e.QuerySingleAsync(_unitOfWorkMock.Object, sql, It.IsAny<Action<NpgsqlParameterCollection>>(), It.IsAny<Func<NpgsqlDataReader, GroupJoinRequest>>()));
        foreach (var result in results)
            sequence = sequence.ReturnsAsync(result);
    }

    /// <summary>A statement run on its own, outside any transaction, answering <paramref name="result"/>.</summary>
    private void SetupRead(string sql, GroupJoinRequest? result)
    {
        _sqlExecutorMock
            .Setup(e => e.QuerySingleAsync(sql, It.IsAny<Action<NpgsqlParameterCollection>>(), It.IsAny<Func<NpgsqlDataReader, GroupJoinRequest>>()))
            .ReturnsAsync(result);
    }

    private static NpgsqlParameterCollection Apply(Action<NpgsqlParameterCollection> configure)
    {
        var command = new NpgsqlCommand();
        configure(command.Parameters);
        return command.Parameters;
    }

    [Test]
    public void GroupJoinRequestRepository_Should_Implement_IGroupJoinRequestRepository()
    {
        CreateRepository().ShouldBeAssignableTo<IGroupJoinRequestRepository>();
    }

    [Test]
    public async Task SubmitAsync_Should_ReturnANewRequest_When_NoneIsOpen()
    {
        var added = _fixture.Create<GroupJoinRequest>();
        SetupSingle(QueryGroupJoinRequests.AddSql, added);

        var result = await CreateRepository().SubmitAsync(added.GroupId, added.PersonId, Audit);

        result.Request.ShouldBe(added);
        result.IsNew.ShouldBeTrue();
        _sqlExecutorMock.Verify(e => e.QuerySingleAsync(_unitOfWorkMock.Object, QueryGroupJoinRequests.GetOpenSql, It.IsAny<Action<NpgsqlParameterCollection>>(), It.IsAny<Func<NpgsqlDataReader, GroupJoinRequest>>()), Times.Never);
    }

    [TestCase(GroupJoinRequestStatus.Pending)]
    [TestCase(GroupJoinRequestStatus.Ignored)]
    public async Task SubmitAsync_Should_AbsorbTheRepeat_When_ARequestIsOpen(GroupJoinRequestStatus openStatus)
    {
        var open = _fixture.Create<GroupJoinRequest>() with { Status = openStatus };
        SetupSingle(QueryGroupJoinRequests.AddSql, (GroupJoinRequest?)null);
        SetupSingle(QueryGroupJoinRequests.GetOpenSql, open);

        var result = await CreateRepository().SubmitAsync(open.GroupId, open.PersonId, Audit);

        result.Request.ShouldBe(open);
        result.IsNew.ShouldBeFalse();
    }

    [Test]
    public async Task SubmitAsync_Should_TryAgain_When_TheOpenRequestWasAnsweredInBetween()
    {
        var added = _fixture.Create<GroupJoinRequest>();
        SetupSingle(QueryGroupJoinRequests.AddSql, null, added);
        SetupSingle(QueryGroupJoinRequests.GetOpenSql, (GroupJoinRequest?)null);

        var result = await CreateRepository().SubmitAsync(added.GroupId, added.PersonId, Audit);

        result.Request.ShouldBe(added);
        result.IsNew.ShouldBeTrue();
    }

    [Test]
    public async Task SubmitAsync_Should_Throw_When_ItNeitherInsertsNorFindsOne()
    {
        _sqlExecutorMock
            .Setup(e => e.QuerySingleAsync(_unitOfWorkMock.Object, It.IsAny<string>(), It.IsAny<Action<NpgsqlParameterCollection>>(), It.IsAny<Func<NpgsqlDataReader, GroupJoinRequest>>()))
            .ReturnsAsync((GroupJoinRequest?)null);

        await Should.ThrowAsync<InvalidOperationException>(() => CreateRepository().SubmitAsync(_fixture.Create<int>(), _fixture.Create<int>(), Audit));
        _unitOfWorkMock.Verify(u => u.RollbackAsync(), Times.Once);
        _unitOfWorkMock.Verify(u => u.CommitAsync(), Times.Never);
    }

    [Test]
    public async Task SubmitAsync_Should_RecordANewRequest_InItsTransaction()
    {
        var added = _fixture.Create<GroupJoinRequest>();
        SetupSingle(QueryGroupJoinRequests.AddSql, added);

        await CreateRepository().SubmitAsync(added.GroupId, added.PersonId, Audit);

        _auditMock.Verify(a => a.RecordAsync(_unitOfWorkMock.Object, added.GroupId,
            It.Is<AuditEntry>(e => e.Kind == "request.queued" && e.Parameters!.Contains(added.GroupJoinRequestUuid.ToString()))), Times.Once);
        _unitOfWorkMock.Verify(u => u.BeginTransactionAsync(), Times.Once);
        _unitOfWorkMock.Verify(u => u.CommitAsync(), Times.Once);
    }

    [Test]
    public async Task SubmitAsync_Should_RecordNothing_When_TheRepeatIsAbsorbed()
    {
        var open = _fixture.Create<GroupJoinRequest>();
        SetupSingle(QueryGroupJoinRequests.AddSql, (GroupJoinRequest?)null);
        SetupSingle(QueryGroupJoinRequests.GetOpenSql, open);

        await CreateRepository().SubmitAsync(open.GroupId, open.PersonId, Audit);

        _auditMock.Verify(a => a.RecordAsync(It.IsAny<IUnitOfWork>(), It.IsAny<int?>(), It.IsAny<AuditEntry>()), Times.Never);
        _unitOfWorkMock.Verify(u => u.CommitAsync(), Times.Once);
    }

    [Test]
    public async Task SubmitAsync_Should_PassTheGroupAndPerson()
    {
        Action<NpgsqlParameterCollection>? captured = null;
        var groupId = _fixture.Create<int>();
        var personId = _fixture.Create<int>();
        _sqlExecutorMock
            .Setup(e => e.QuerySingleAsync(_unitOfWorkMock.Object, QueryGroupJoinRequests.AddSql, It.IsAny<Action<NpgsqlParameterCollection>>(), It.IsAny<Func<NpgsqlDataReader, GroupJoinRequest>>()))
            .Callback<IUnitOfWork, string, Action<NpgsqlParameterCollection>, Func<NpgsqlDataReader, GroupJoinRequest>>((_, _, configure, _) => captured = configure)
            .ReturnsAsync(_fixture.Create<GroupJoinRequest>());

        await CreateRepository().SubmitAsync(groupId, personId, Audit);

        var parameters = Apply(captured!);
        parameters[pn.GroupId].Value.ShouldBe(groupId);
        parameters[pn.PersonId].Value.ShouldBe(personId);
    }

    [Test]
    public void AddSql_Should_AbsorbRepeatsOnPendingAndIgnored()
    {
        QueryGroupJoinRequests.AddSql.ShouldContain(@"ON CONFLICT (""GroupId"", ""PersonId"") WHERE ""Status"" IN (0, 3) DO NOTHING");
    }

    [Test]
    public async Task GetByUuidAsync_Should_ReturnTheRequest()
    {
        var request = _fixture.Create<GroupJoinRequest>();
        SetupRead(QueryGroupJoinRequests.GetByUuidSql, request);

        var result = await CreateRepository().GetByUuidAsync(request.GroupJoinRequestUuid);

        result.ShouldBe(request);
    }

    [Test]
    public async Task ListPendingByGroupAsync_Should_ReturnThePendingRequests()
    {
        var pending = _fixture.CreateMany<PendingGroupJoinRequest>(3).ToList();
        _sqlExecutorMock
            .Setup(e => e.QueryManyAsync(QueryGroupJoinRequests.ListPendingByGroupSql, It.IsAny<Action<NpgsqlParameterCollection>>(), It.IsAny<Func<NpgsqlDataReader, PendingGroupJoinRequest>>()))
            .ReturnsAsync(pending);

        var result = await CreateRepository().ListPendingByGroupAsync(_fixture.Create<int>());

        result.ShouldBe(pending);
    }

    [TestCase(GroupJoinRequestStatus.Accepted)]
    [TestCase(GroupJoinRequestStatus.Rejected)]
    [TestCase(GroupJoinRequestStatus.Ignored)]
    public async Task AnswerAsync_Should_RecordTheAnswerWhoAndWhen(GroupJoinRequestStatus answer)
    {
        Action<NpgsqlParameterCollection>? captured = null;
        var answered = _fixture.Create<GroupJoinRequest>() with { Status = answer };
        var answeredBy = _fixture.Create<int>();
        _sqlExecutorMock
            .Setup(e => e.QuerySingleAsync(_unitOfWorkMock.Object, QueryGroupJoinRequests.AnswerSql, It.IsAny<Action<NpgsqlParameterCollection>>(), It.IsAny<Func<NpgsqlDataReader, GroupJoinRequest>>()))
            .Callback<IUnitOfWork, string, Action<NpgsqlParameterCollection>, Func<NpgsqlDataReader, GroupJoinRequest>>((_, _, configure, _) => captured = configure)
            .ReturnsAsync(answered);

        var before = DateTimeOffset.UtcNow;
        var result = await CreateRepository().AnswerAsync(answered.GroupId, answered.GroupJoinRequestUuid, answer, answeredBy, Answer);

        result.ShouldBe(answered);
        var parameters = Apply(captured!);
        parameters[pn.GroupId].Value.ShouldBe(answered.GroupId);
        parameters[pn.GroupJoinRequestUuid].Value.ShouldBe(answered.GroupJoinRequestUuid);
        parameters[pn.Status].Value.ShouldBe((int)answer);
        parameters[pn.AnsweredByPersonId].Value.ShouldBe(answeredBy);
        ((DateTimeOffset)parameters[pn.Now].Value!).ShouldBeGreaterThanOrEqualTo(before);
    }

    [Test]
    public async Task AnswerAsync_Should_ReturnNull_AndRecordNothing_When_NothingPendingMatches()
    {
        SetupSingle(QueryGroupJoinRequests.AnswerSql, (GroupJoinRequest?)null);

        var result = await CreateRepository().AnswerAsync(_fixture.Create<int>(), Guid.NewGuid(), GroupJoinRequestStatus.Accepted, _fixture.Create<int>(), Answer);

        result.ShouldBeNull();
        _auditMock.Verify(a => a.RecordAsync(It.IsAny<IUnitOfWork>(), It.IsAny<int?>(), It.IsAny<AuditEntry>()), Times.Never);
        _unitOfWorkMock.Verify(u => u.CommitAsync(), Times.Never);
    }

    [Test]
    public async Task AnswerAsync_Should_RecordItAboutTheAsker_AndCloseItsNotifications_InOneTransaction()
    {
        var answered = _fixture.Create<GroupJoinRequest>() with { Status = GroupJoinRequestStatus.Rejected };
        SetupSingle(QueryGroupJoinRequests.AnswerSql, answered);

        await CreateRepository().AnswerAsync(answered.GroupId, answered.GroupJoinRequestUuid, GroupJoinRequestStatus.Rejected, 7, Answer);

        _auditMock.Verify(a => a.RecordAsync(_unitOfWorkMock.Object, answered.GroupId, It.Is<AuditEntry>(e =>
            e.Kind == "request.declined" && e.SubjectPersonId == answered.PersonId && e.ActorPersonId == 7
            && e.Parameters!.Contains(answered.GroupJoinRequestUuid.ToString()))), Times.Once);
        _sqlExecutorMock.Verify(e => e.QuerySingleValueAsync(_unitOfWorkMock.Object, QueryGroupJoinRequests.CloseNotificationsSql,
            It.IsAny<Action<NpgsqlParameterCollection>>(), It.IsAny<Func<NpgsqlDataReader, long>>()), Times.Once);
        _unitOfWorkMock.Verify(u => u.CommitAsync(), Times.Once);
    }

    [Test]
    public async Task AnswerAsync_Should_KeepWhatTheEntryCarries_BesideTheRequest()
    {
        var answered = _fixture.Create<GroupJoinRequest>() with { Status = GroupJoinRequestStatus.Rejected };
        SetupSingle(QueryGroupJoinRequests.AnswerSql, answered);

        await CreateRepository().AnswerAsync(answered.GroupId, answered.GroupJoinRequestUuid, GroupJoinRequestStatus.Rejected, 7,
            Answer with { Parameters = "{\"note\":\"We only add family.\"}" });

        _auditMock.Verify(a => a.RecordAsync(_unitOfWorkMock.Object, answered.GroupId, It.Is<AuditEntry>(e =>
            e.Parameters!.Contains("\"note\":\"We only add family.\"") && e.Parameters.Contains(answered.GroupJoinRequestUuid.ToString()))), Times.Once);
    }

    [Test]
    public void CloseNotificationsSql_Should_CloseOnlyOpenNotificationsOfThatRequest()
    {
        var sql = QueryGroupJoinRequests.CloseNotificationsSql;

        sql.ShouldContain("'request.queued'");
        sql.ShouldContain("->> 'requestUuid')::uuid = @GroupJoinRequestUuid");
        sql.ShouldContain(@"""Status"" IN (0, 1)");
        sql.ShouldContain(@"""Status"" = 3");
    }

    [TestCase(GroupJoinRequestStatus.Pending)]
    [TestCase((GroupJoinRequestStatus)42)]
    public async Task AnswerAsync_Should_Throw_When_TheAnswerIsNotAnAnswer(GroupJoinRequestStatus answer)
    {
        await Should.ThrowAsync<ArgumentOutOfRangeException>(() => CreateRepository().AnswerAsync(_fixture.Create<int>(), Guid.NewGuid(), answer, _fixture.Create<int>(), Answer));

        _sqlExecutorMock.VerifyNoOtherCalls();
    }

    [Test]
    public void AnswerSql_Should_OnlyChangeAPendingRequestOfTheNamedGroup()
    {
        var sql = QueryGroupJoinRequests.AnswerSql;

        sql.ShouldContain(@"""GroupId"" = @GroupId");
        sql.ShouldContain(@"""Status"" = 0");
    }

    [Test]
    public async Task ListPendingByGroupAsync_Should_Rethrow_When_ExecutorThrows()
    {
        _sqlExecutorMock
            .Setup(e => e.QueryManyAsync(QueryGroupJoinRequests.ListPendingByGroupSql, It.IsAny<Action<NpgsqlParameterCollection>>(), It.IsAny<Func<NpgsqlDataReader, PendingGroupJoinRequest>>()))
            .ThrowsAsync(new InvalidOperationException("boom"));

        await Should.ThrowAsync<InvalidOperationException>(() => CreateRepository().ListPendingByGroupAsync(_fixture.Create<int>()));
    }
}
