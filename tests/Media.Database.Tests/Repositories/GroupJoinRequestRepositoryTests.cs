#nullable enable
using AutoFixture;
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
    private IFixture _fixture = null!;

    [SetUp]
    public void Setup()
    {
        _fixture = AutoMoqFixture.Create();
        _sqlExecutorMock = _fixture.Freeze<Mock<ISqlQueryExecutor>>();
    }

    private GroupJoinRequestRepository CreateRepository() => new(
        _sqlExecutorMock.Object,
        Mock.Of<ILogger<GroupJoinRequestRepository>>());

    private void SetupSingle(string sql, params GroupJoinRequest?[] results)
    {
        var sequence = _sqlExecutorMock.SetupSequence(e => e.QuerySingleAsync(sql, It.IsAny<Action<NpgsqlParameterCollection>>(), It.IsAny<Func<NpgsqlDataReader, GroupJoinRequest>>()));
        foreach (var result in results)
            sequence = sequence.ReturnsAsync(result);
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

        var result = await CreateRepository().SubmitAsync(added.GroupId, added.PersonId);

        result.Request.ShouldBe(added);
        result.IsNew.ShouldBeTrue();
        _sqlExecutorMock.Verify(e => e.QuerySingleAsync(QueryGroupJoinRequests.GetOpenSql, It.IsAny<Action<NpgsqlParameterCollection>>(), It.IsAny<Func<NpgsqlDataReader, GroupJoinRequest>>()), Times.Never);
    }

    [TestCase(GroupJoinRequestStatus.Pending)]
    [TestCase(GroupJoinRequestStatus.Ignored)]
    public async Task SubmitAsync_Should_AbsorbTheRepeat_When_ARequestIsOpen(GroupJoinRequestStatus openStatus)
    {
        var open = _fixture.Create<GroupJoinRequest>() with { Status = openStatus };
        SetupSingle(QueryGroupJoinRequests.AddSql, (GroupJoinRequest?)null);
        SetupSingle(QueryGroupJoinRequests.GetOpenSql, open);

        var result = await CreateRepository().SubmitAsync(open.GroupId, open.PersonId);

        result.Request.ShouldBe(open);
        result.IsNew.ShouldBeFalse();
    }

    [Test]
    public async Task SubmitAsync_Should_TryAgain_When_TheOpenRequestWasAnsweredInBetween()
    {
        var added = _fixture.Create<GroupJoinRequest>();
        SetupSingle(QueryGroupJoinRequests.AddSql, null, added);
        SetupSingle(QueryGroupJoinRequests.GetOpenSql, (GroupJoinRequest?)null);

        var result = await CreateRepository().SubmitAsync(added.GroupId, added.PersonId);

        result.Request.ShouldBe(added);
        result.IsNew.ShouldBeTrue();
    }

    [Test]
    public async Task SubmitAsync_Should_Throw_When_ItNeitherInsertsNorFindsOne()
    {
        _sqlExecutorMock
            .Setup(e => e.QuerySingleAsync(It.IsAny<string>(), It.IsAny<Action<NpgsqlParameterCollection>>(), It.IsAny<Func<NpgsqlDataReader, GroupJoinRequest>>()))
            .ReturnsAsync((GroupJoinRequest?)null);

        await Should.ThrowAsync<InvalidOperationException>(() => CreateRepository().SubmitAsync(_fixture.Create<int>(), _fixture.Create<int>()));
    }

    [Test]
    public async Task SubmitAsync_Should_PassTheGroupAndPerson()
    {
        Action<NpgsqlParameterCollection>? captured = null;
        var groupId = _fixture.Create<int>();
        var personId = _fixture.Create<int>();
        _sqlExecutorMock
            .Setup(e => e.QuerySingleAsync(QueryGroupJoinRequests.AddSql, It.IsAny<Action<NpgsqlParameterCollection>>(), It.IsAny<Func<NpgsqlDataReader, GroupJoinRequest>>()))
            .Callback<string, Action<NpgsqlParameterCollection>, Func<NpgsqlDataReader, GroupJoinRequest>>((_, configure, _) => captured = configure)
            .ReturnsAsync(_fixture.Create<GroupJoinRequest>());

        await CreateRepository().SubmitAsync(groupId, personId);

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
        SetupSingle(QueryGroupJoinRequests.GetByUuidSql, request);

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
            .Setup(e => e.QuerySingleAsync(QueryGroupJoinRequests.AnswerSql, It.IsAny<Action<NpgsqlParameterCollection>>(), It.IsAny<Func<NpgsqlDataReader, GroupJoinRequest>>()))
            .Callback<string, Action<NpgsqlParameterCollection>, Func<NpgsqlDataReader, GroupJoinRequest>>((_, configure, _) => captured = configure)
            .ReturnsAsync(answered);

        var before = DateTimeOffset.UtcNow;
        var result = await CreateRepository().AnswerAsync(answered.GroupId, answered.GroupJoinRequestUuid, answer, answeredBy);

        result.ShouldBe(answered);
        var parameters = Apply(captured!);
        parameters[pn.GroupId].Value.ShouldBe(answered.GroupId);
        parameters[pn.GroupJoinRequestUuid].Value.ShouldBe(answered.GroupJoinRequestUuid);
        parameters[pn.Status].Value.ShouldBe((int)answer);
        parameters[pn.AnsweredByPersonId].Value.ShouldBe(answeredBy);
        ((DateTimeOffset)parameters[pn.Now].Value!).ShouldBeGreaterThanOrEqualTo(before);
    }

    [Test]
    public async Task AnswerAsync_Should_ReturnNull_When_NothingPendingMatches()
    {
        SetupSingle(QueryGroupJoinRequests.AnswerSql, (GroupJoinRequest?)null);

        var result = await CreateRepository().AnswerAsync(_fixture.Create<int>(), Guid.NewGuid(), GroupJoinRequestStatus.Accepted, _fixture.Create<int>());

        result.ShouldBeNull();
    }

    [TestCase(GroupJoinRequestStatus.Pending)]
    [TestCase((GroupJoinRequestStatus)42)]
    public async Task AnswerAsync_Should_Throw_When_TheAnswerIsNotAnAnswer(GroupJoinRequestStatus answer)
    {
        await Should.ThrowAsync<ArgumentOutOfRangeException>(() => CreateRepository().AnswerAsync(_fixture.Create<int>(), Guid.NewGuid(), answer, _fixture.Create<int>()));

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
