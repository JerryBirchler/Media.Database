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
using System.Threading.Tasks;
#pragma warning disable CS8981
using pn = Media.Database.Repositories.Schemas.ParameterNames;
#pragma warning restore CS8981

namespace Media.Database.Tests.Repositories;

/// <summary>
/// How PersonMemberAddStrikeRepository stores what <see cref="MemberAddStrikePolicy"/> decides: the
/// first miss inserts, later ones update compare-and-set against the row they were computed from,
/// and a lost race reads again. The rule itself is covered in MemberAddStrikePolicyTests.
/// </summary>
[TestFixture]
public class PersonMemberAddStrikeRepositoryTests
{
    private static readonly TimeSpan OneDay = TimeSpan.FromDays(1);

    private Mock<ISqlQueryExecutor> _sqlExecutorMock = null!;
    private IFixture _fixture = null!;

    [SetUp]
    public void Setup()
    {
        _fixture = AutoMoqFixture.Create();
        _sqlExecutorMock = _fixture.Freeze<Mock<ISqlQueryExecutor>>();
    }

    private PersonMemberAddStrikeRepository CreateRepository() => new(
        _sqlExecutorMock.Object,
        Mock.Of<ILogger<PersonMemberAddStrikeRepository>>());

    private void SetupGet(params PersonMemberAddStrikes?[] results)
    {
        var sequence = _sqlExecutorMock.SetupSequence(e => e.QuerySingleAsync(QueryPersonMemberAddStrikes.GetByPersonIdSql, It.IsAny<Action<NpgsqlParameterCollection>>(), It.IsAny<Func<NpgsqlDataReader, PersonMemberAddStrikes>>()));
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
    public void PersonMemberAddStrikeRepository_Should_Implement_IPersonMemberAddStrikeRepository()
    {
        CreateRepository().ShouldBeAssignableTo<IPersonMemberAddStrikeRepository>();
    }

    [Test]
    public async Task GetAsync_Should_ReturnTheRow()
    {
        var strikes = _fixture.Create<PersonMemberAddStrikes>();
        SetupGet(strikes);

        var result = await CreateRepository().GetAsync(strikes.PersonId);

        result.ShouldBe(strikes);
    }

    [Test]
    public async Task RecordMissAsync_Should_InsertTheFirstStrike_When_ThereIsNoRow()
    {
        var personId = _fixture.Create<int>();
        Action<NpgsqlParameterCollection>? captured = null;
        var stored = _fixture.Create<PersonMemberAddStrikes>();
        SetupGet((PersonMemberAddStrikes?)null);
        _sqlExecutorMock
            .Setup(e => e.QuerySingleAsync(QueryPersonMemberAddStrikes.InsertIfAbsentSql, It.IsAny<Action<NpgsqlParameterCollection>>(), It.IsAny<Func<NpgsqlDataReader, PersonMemberAddStrikes>>()))
            .Callback<string, Action<NpgsqlParameterCollection>, Func<NpgsqlDataReader, PersonMemberAddStrikes>>((_, configure, _) => captured = configure)
            .ReturnsAsync(stored);

        var before = DateTimeOffset.UtcNow;
        var result = await CreateRepository().RecordMissAsync(personId, OneDay);

        result.ShouldBe(stored);
        var parameters = Apply(captured!);
        parameters[pn.PersonId].Value.ShouldBe(personId);
        parameters[pn.StrikeCount].Value.ShouldBe(1);
        var now = (DateTimeOffset)parameters[pn.Now].Value!;
        now.ShouldBeGreaterThanOrEqualTo(before);
        parameters[pn.WindowEndsOn].Value.ShouldBe(now + OneDay);
        parameters[pn.LockedOn].Value.ShouldBe(DBNull.Value);
        _sqlExecutorMock.Verify(e => e.QuerySingleAsync(QueryPersonMemberAddStrikes.UpdateIfUnchangedSql, It.IsAny<Action<NpgsqlParameterCollection>>(), It.IsAny<Func<NpgsqlDataReader, PersonMemberAddStrikes>>()), Times.Never);
    }

    [Test]
    public async Task RecordMissAsync_Should_UpdateAgainstTheRowItRead_When_InsideTheWindow()
    {
        var current = new PersonMemberAddStrikes
        {
            PersonId = _fixture.Create<int>(),
            StrikeCount = 2,
            WindowEndsOn = DateTimeOffset.UtcNow.AddDays(1),
            InsertedOn = DateTimeOffset.UtcNow.AddDays(-1)
        };
        Action<NpgsqlParameterCollection>? captured = null;
        var stored = current with { StrikeCount = 3, LockedOn = DateTimeOffset.UtcNow };
        SetupGet(current);
        _sqlExecutorMock
            .Setup(e => e.QuerySingleAsync(QueryPersonMemberAddStrikes.UpdateIfUnchangedSql, It.IsAny<Action<NpgsqlParameterCollection>>(), It.IsAny<Func<NpgsqlDataReader, PersonMemberAddStrikes>>()))
            .Callback<string, Action<NpgsqlParameterCollection>, Func<NpgsqlDataReader, PersonMemberAddStrikes>>((_, configure, _) => captured = configure)
            .ReturnsAsync(stored);

        var result = await CreateRepository().RecordMissAsync(current.PersonId, OneDay);

        result.ShouldBe(stored);
        var parameters = Apply(captured!);
        var now = (DateTimeOffset)parameters[pn.Now].Value!;
        parameters[pn.StrikeCount].Value.ShouldBe(3);
        parameters[pn.WindowEndsOn].Value.ShouldBe(now + TimeSpan.FromDays(4));
        parameters[pn.LockedOn].Value.ShouldBe(now);
        parameters[pn.ExpectedStrikeCount].Value.ShouldBe(2);
        parameters[pn.ExpectedWindowEndsOn].Value.ShouldBe(current.WindowEndsOn);
    }

    [Test]
    public async Task RecordMissAsync_Should_ReadAgainAndRetry_When_TheRowChangedMeanwhile()
    {
        var personId = _fixture.Create<int>();
        var stale = new PersonMemberAddStrikes { PersonId = personId, StrikeCount = 1, WindowEndsOn = DateTimeOffset.UtcNow.AddDays(1) };
        var fresh = stale with { StrikeCount = 2, WindowEndsOn = DateTimeOffset.UtcNow.AddDays(2) };
        var stored = fresh with { StrikeCount = 3 };
        var expectedCounts = new System.Collections.Generic.List<object?>();
        SetupGet(stale, fresh);
        // The first write finds the row changed (no row back); the second, after reading again, lands.
        var returns = new System.Collections.Generic.Queue<PersonMemberAddStrikes?>([null, stored]);
        _sqlExecutorMock
            .Setup(e => e.QuerySingleAsync(QueryPersonMemberAddStrikes.UpdateIfUnchangedSql, It.IsAny<Action<NpgsqlParameterCollection>>(), It.IsAny<Func<NpgsqlDataReader, PersonMemberAddStrikes>>()))
            .Callback<string, Action<NpgsqlParameterCollection>, Func<NpgsqlDataReader, PersonMemberAddStrikes>>((_, configure, _) => expectedCounts.Add(Apply(configure)[pn.ExpectedStrikeCount].Value))
            .ReturnsAsync(() => returns.Dequeue());

        var result = await CreateRepository().RecordMissAsync(personId, OneDay);

        result.ShouldBe(stored);
        expectedCounts.ShouldBe([1, 2]);
    }

    [Test]
    public async Task RecordMissAsync_Should_FallBackToUpdate_When_TheFirstInsertLostTheRace()
    {
        var personId = _fixture.Create<int>();
        var appeared = new PersonMemberAddStrikes { PersonId = personId, StrikeCount = 1, WindowEndsOn = DateTimeOffset.UtcNow.AddDays(1) };
        var stored = appeared with { StrikeCount = 2 };
        SetupGet(null, appeared);
        _sqlExecutorMock
            .Setup(e => e.QuerySingleAsync(QueryPersonMemberAddStrikes.InsertIfAbsentSql, It.IsAny<Action<NpgsqlParameterCollection>>(), It.IsAny<Func<NpgsqlDataReader, PersonMemberAddStrikes>>()))
            .ReturnsAsync((PersonMemberAddStrikes?)null);
        _sqlExecutorMock
            .Setup(e => e.QuerySingleAsync(QueryPersonMemberAddStrikes.UpdateIfUnchangedSql, It.IsAny<Action<NpgsqlParameterCollection>>(), It.IsAny<Func<NpgsqlDataReader, PersonMemberAddStrikes>>()))
            .ReturnsAsync(stored);

        var result = await CreateRepository().RecordMissAsync(personId, OneDay);

        result.ShouldBe(stored);
    }

    [Test]
    public async Task RecordMissAsync_Should_Throw_When_EveryAttemptLosesTheRace()
    {
        var current = new PersonMemberAddStrikes { PersonId = _fixture.Create<int>(), StrikeCount = 1, WindowEndsOn = DateTimeOffset.UtcNow.AddDays(1) };
        _sqlExecutorMock
            .Setup(e => e.QuerySingleAsync(QueryPersonMemberAddStrikes.GetByPersonIdSql, It.IsAny<Action<NpgsqlParameterCollection>>(), It.IsAny<Func<NpgsqlDataReader, PersonMemberAddStrikes>>()))
            .ReturnsAsync(current);
        _sqlExecutorMock
            .Setup(e => e.QuerySingleAsync(QueryPersonMemberAddStrikes.UpdateIfUnchangedSql, It.IsAny<Action<NpgsqlParameterCollection>>(), It.IsAny<Func<NpgsqlDataReader, PersonMemberAddStrikes>>()))
            .ReturnsAsync((PersonMemberAddStrikes?)null);

        await Should.ThrowAsync<InvalidOperationException>(() => CreateRepository().RecordMissAsync(current.PersonId, OneDay));

        _sqlExecutorMock.Verify(e => e.QuerySingleAsync(QueryPersonMemberAddStrikes.UpdateIfUnchangedSql, It.IsAny<Action<NpgsqlParameterCollection>>(), It.IsAny<Func<NpgsqlDataReader, PersonMemberAddStrikes>>()),
            Times.Exactly(PersonMemberAddStrikeRepository.RecordMissAttempts));
    }

    [Test]
    public async Task RecordMissAsync_Should_Throw_When_TheBaseWindowIsNotPositive()
    {
        await Should.ThrowAsync<ArgumentOutOfRangeException>(() => CreateRepository().RecordMissAsync(_fixture.Create<int>(), TimeSpan.Zero));

        _sqlExecutorMock.VerifyNoOtherCalls();
    }

    [Test]
    public async Task ClearAsync_Should_ReturnTheClearedRow()
    {
        var cleared = _fixture.Create<PersonMemberAddStrikes>() with { StrikeCount = 0, WindowEndsOn = null, LockedOn = null };
        Action<NpgsqlParameterCollection>? captured = null;
        _sqlExecutorMock
            .Setup(e => e.QuerySingleAsync(QueryPersonMemberAddStrikes.ClearSql, It.IsAny<Action<NpgsqlParameterCollection>>(), It.IsAny<Func<NpgsqlDataReader, PersonMemberAddStrikes>>()))
            .Callback<string, Action<NpgsqlParameterCollection>, Func<NpgsqlDataReader, PersonMemberAddStrikes>>((_, configure, _) => captured = configure)
            .ReturnsAsync(cleared);

        var result = await CreateRepository().ClearAsync(cleared.PersonId);

        result.ShouldBe(cleared);
        Apply(captured!)[pn.PersonId].Value.ShouldBe(cleared.PersonId);
    }

    [Test]
    public void ClearSql_Should_ClearTheLockToo()
    {
        QueryPersonMemberAddStrikes.ClearSql.ShouldContain(@"""LockedOn"" = NULL");
    }

    [Test]
    public async Task ResetEndedWindowsAsync_Should_ReturnHowManyWereReset()
    {
        var count = _fixture.Create<int>();
        _sqlExecutorMock
            .Setup(e => e.ExecuteAsync(QueryPersonMemberAddStrikes.ResetEndedWindowsSql, It.IsAny<Action<NpgsqlParameterCollection>>()))
            .ReturnsAsync(count);

        var result = await CreateRepository().ResetEndedWindowsAsync();

        result.ShouldBe(count);
    }

    [Test]
    public void ResetEndedWindowsSql_Should_LeaveLocksAlone()
    {
        QueryPersonMemberAddStrikes.ResetEndedWindowsSql.ShouldNotContain("LockedOn");
    }

    [Test]
    public async Task ResetEndedWindowsAsync_Should_Rethrow_When_ExecutorThrows()
    {
        _sqlExecutorMock
            .Setup(e => e.ExecuteAsync(QueryPersonMemberAddStrikes.ResetEndedWindowsSql, It.IsAny<Action<NpgsqlParameterCollection>>()))
            .ThrowsAsync(new InvalidOperationException("boom"));

        await Should.ThrowAsync<InvalidOperationException>(() => CreateRepository().ResetEndedWindowsAsync());
    }
}
