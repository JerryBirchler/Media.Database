#nullable enable
using Media.Database.Models;
using Media.Database.Repositories;
using Media.Database.Repositories.Queries;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using Shouldly;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
#pragma warning disable CS8981
using pn = Media.Database.Repositories.Schemas.ParameterNames;
#pragma warning restore CS8981

namespace Media.Database.Tests.Repositories;

[TestFixture]
public class OtpAttemptRepositoryTests
{
    private Mock<ICqlQueryExecutor> _cqlExecutorMock = null!;

    [SetUp]
    public void Setup()
    {
        _cqlExecutorMock = new Mock<ICqlQueryExecutor>();
    }

    private OtpAttemptRepository CreateRepository() => new(
        _cqlExecutorMock.Object,
        Mock.Of<ILogger<OtpAttemptRepository>>());

    private static OtpAttempt Attempt(int attempts = 1, bool isUsed = false) => new()
    {
        NonceId = "nonce:email",
        Attempts = attempts,
        IsUsed = isUsed,
        IssuedOn = new DateTimeOffset(2026, 9, 24, 12, 0, 0, TimeSpan.Zero)
    };

    private Dictionary<string, object> CaptureSaveParameters(TimeSpan timeToLive, OtpAttempt attempt)
    {
        Action<Dictionary<string, object>>? captured = null;
        _cqlExecutorMock
            .Setup(e => e.ExecuteAsync(QueryOtpAttempts.UpsertSql, It.IsAny<Action<Dictionary<string, object>>>()))
            .Callback<string, Action<Dictionary<string, object>>>((_, configure) => captured = configure);

        CreateRepository().SaveAsync(attempt, timeToLive).GetAwaiter().GetResult();

        var parameters = new Dictionary<string, object>();
        captured!(parameters);
        return parameters;
    }

    [Test]
    public void OtpAttemptRepository_Should_Implement_IOtpAttemptRepository()
    {
        CreateRepository().ShouldBeAssignableTo<IOtpAttemptRepository>();
    }

    [Test]
    public void SaveAsync_Should_ConfigureParametersIncludingTheTimeToLive()
    {
        var attempt = Attempt(attempts: 2, isUsed: true);

        var parameters = CaptureSaveParameters(TimeSpan.FromMinutes(90), attempt);

        parameters[pn.NonceId.ToUpperInvariant()].ShouldBe(attempt.NonceId);
        parameters[pn.Attempts.ToUpperInvariant()].ShouldBe(2);
        parameters[pn.IsUsed.ToUpperInvariant()].ShouldBe(true);
        parameters[pn.IssuedOn.ToUpperInvariant()].ShouldBe(attempt.IssuedOn);
        parameters[pn.TimeToLiveSeconds.ToUpperInvariant()].ShouldBe(5400);
    }

    // A counter must always expire: a window already closed still gets the shortest real TTL.
    [TestCase(0)]
    [TestCase(-30)]
    public void SaveAsync_Should_ClampANonPositiveTimeToLiveToOneSecond(int seconds)
    {
        var parameters = CaptureSaveParameters(TimeSpan.FromSeconds(seconds), Attempt());

        parameters[pn.TimeToLiveSeconds.ToUpperInvariant()].ShouldBe(1);
    }

    [Test]
    public void SaveAsync_Should_RoundAPartialSecondUp()
    {
        var parameters = CaptureSaveParameters(TimeSpan.FromMilliseconds(1500), Attempt());

        parameters[pn.TimeToLiveSeconds.ToUpperInvariant()].ShouldBe(2);
    }

    [Test]
    public void UpsertSql_Should_BindTheTimeToLive()
    {
        QueryOtpAttempts.UpsertSql.ShouldContain($"USING TTL {pn.TimeToLiveSeconds}");
    }

    [Test]
    public async Task GetAsync_Should_ReturnWhatTheExecutorReturns()
    {
        var expected = Attempt();
        _cqlExecutorMock
            .Setup(e => e.QuerySingleAsync(QueryOtpAttempts.GetSql, It.IsAny<Action<Dictionary<string, object>>>(), It.IsAny<Func<Cassandra.Row, OtpAttempt>>()))
            .ReturnsAsync(expected);

        (await CreateRepository().GetAsync(expected.NonceId)).ShouldBe(expected);
    }

    [Test]
    public void GetAsync_Should_Rethrow_When_ExecutorThrows()
    {
        _cqlExecutorMock
            .Setup(e => e.QuerySingleAsync(It.IsAny<string>(), It.IsAny<Action<Dictionary<string, object>>>(), It.IsAny<Func<Cassandra.Row, OtpAttempt>>()))
            .ThrowsAsync(new InvalidOperationException("boom"));

        Should.ThrowAsync<InvalidOperationException>(() => CreateRepository().GetAsync("n"));
    }

    [Test]
    public void SaveAsync_Should_Rethrow_When_ExecutorThrows()
    {
        _cqlExecutorMock
            .Setup(e => e.ExecuteAsync(It.IsAny<string>(), It.IsAny<Action<Dictionary<string, object>>>()))
            .ThrowsAsync(new InvalidOperationException("boom"));

        Should.ThrowAsync<InvalidOperationException>(() => CreateRepository().SaveAsync(Attempt(), TimeSpan.FromMinutes(1)));
    }
}
