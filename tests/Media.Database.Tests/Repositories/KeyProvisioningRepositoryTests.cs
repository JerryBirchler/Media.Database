#nullable enable
using AutoFixture;
using Media.Database.Models;
using Media.Database.Repositories;
using Media.Database.Repositories.Queries;
using Media.Database.Tests.TestHelpers;
using Moq;
using Npgsql;
using NUnit.Framework;
using Shouldly;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using pn = Media.Database.Repositories.Schemas.ParameterNames;

namespace Media.Database.Tests.Repositories;

[TestFixture]
public class KeyProvisioningRepositoryTests
{
    private IFixture _fixture = null!;
    private Mock<ISqlQueryExecutor> _sqlExecutorMock = null!;

    [SetUp]
    public void SetUp()
    {
        _fixture = AutoMoqFixture.Create();
        _sqlExecutorMock = _fixture.Freeze<Mock<ISqlQueryExecutor>>();
    }

    private KeyProvisioningRepository CreateRepository() => _fixture.Create<KeyProvisioningRepository>();

    [Test]
    public async Task GetCandidateAsync_Should_AskByTheDevice_And_ReturnTheCandidate()
    {
        var candidate = _fixture.Create<ProvisioningCandidate>();
        Action<NpgsqlParameterCollection>? captured = null;
        _sqlExecutorMock
            .Setup(e => e.QuerySingleAsync(QueryRegistrations.GetProvisioningCandidateSql, It.IsAny<Action<NpgsqlParameterCollection>>(), It.IsAny<Func<NpgsqlDataReader, ProvisioningCandidate>>()))
            .Callback<string, Action<NpgsqlParameterCollection>, Func<NpgsqlDataReader, ProvisioningCandidate>>((_, configure, _) => captured = configure)
            .ReturnsAsync(candidate);

        (await CreateRepository().GetCandidateAsync(48)).ShouldBe(candidate);

        using var command = new NpgsqlCommand();
        captured!(command.Parameters);
        command.Parameters[pn.SourceMachineId].Value.ShouldBe(48);
    }

    [Test]
    public async Task GetCandidateAsync_Should_ReturnNull_When_NoKeyIsOwed()
    {
        _sqlExecutorMock
            .Setup(e => e.QuerySingleAsync(QueryRegistrations.GetProvisioningCandidateSql, It.IsAny<Action<NpgsqlParameterCollection>>(), It.IsAny<Func<NpgsqlDataReader, ProvisioningCandidate>>()))
            .ReturnsAsync((ProvisioningCandidate?)null);

        (await CreateRepository().GetCandidateAsync(48)).ShouldBeNull();
    }

    [Test]
    public async Task GetCandidatesAsync_Should_PassTheLimit_And_ReturnTheCandidates()
    {
        var candidates = _fixture.CreateMany<ProvisioningCandidate>(2).ToList();
        Action<NpgsqlParameterCollection>? captured = null;
        _sqlExecutorMock
            .Setup(e => e.QueryManyAsync(QueryRegistrations.GetProvisioningCandidatesSql, It.IsAny<Action<NpgsqlParameterCollection>>(), It.IsAny<Func<NpgsqlDataReader, ProvisioningCandidate>>()))
            .Callback<string, Action<NpgsqlParameterCollection>, Func<NpgsqlDataReader, ProvisioningCandidate>>((_, configure, _) => captured = configure)
            .ReturnsAsync(candidates);

        (await CreateRepository().GetCandidatesAsync(25)).ShouldBe(candidates);

        using var command = new NpgsqlCommand();
        captured!(command.Parameters);
        command.Parameters[pn.Limit].Value.ShouldBe(25);
    }

    [Test]
    public async Task BindGroupShellIfUnboundAsync_Should_ConfigureParameters()
    {
        Action<NpgsqlParameterCollection>? captured = null;
        _sqlExecutorMock
            .Setup(e => e.ExecuteAsync(QueryRegistrations.SetGroupShellIdIfUnsetSql, It.IsAny<Action<NpgsqlParameterCollection>>()))
            .Callback<string, Action<NpgsqlParameterCollection>>((_, configure) => captured = configure)
            .ReturnsAsync(1);

        await CreateRepository().BindGroupShellIfUnboundAsync(sourceMachineId: 11, groupShellId: 22);

        using var command = new NpgsqlCommand();
        captured!(command.Parameters);
        command.Parameters[pn.SourceMachineId].Value.ShouldBe(11);
        command.Parameters[pn.GroupShellId].Value.ShouldBe(22);
    }

    // The provisioner delivers only when it knows its shell is the one bound, so it must be told.
    [TestCase(1, true)]
    [TestCase(0, false)]
    public async Task BindGroupShellIfUnboundAsync_Should_ReportWhetherThisCallBoundTheShell(int rowsAffected, bool expected)
    {
        _sqlExecutorMock
            .Setup(e => e.ExecuteAsync(QueryRegistrations.SetGroupShellIdIfUnsetSql, It.IsAny<Action<NpgsqlParameterCollection>>()))
            .ReturnsAsync(rowsAffected);

        (await CreateRepository().BindGroupShellIfUnboundAsync(11, 22)).ShouldBe(expected);
    }

    [Test]
    public async Task BindGroupShellIfUnboundAsync_Should_Rethrow_When_ExecutorThrows()
    {
        _sqlExecutorMock
            .Setup(e => e.ExecuteAsync(QueryRegistrations.SetGroupShellIdIfUnsetSql, It.IsAny<Action<NpgsqlParameterCollection>>()))
            .ThrowsAsync(new InvalidOperationException("boom"));

        await Should.ThrowAsync<InvalidOperationException>(() => CreateRepository().BindGroupShellIfUnboundAsync(1, 2));
    }
}
