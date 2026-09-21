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
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Media.Database.Tests.Repositories;

[TestFixture]
public class SourceMachineKeyRepositoryTests
{
    private Mock<ISqlQueryExecutor> _sqlExecutorMock = null!;
    private IFixture _fixture = null!;

    [SetUp]
    public void Setup()
    {
        _fixture = AutoMoqFixture.Create();
        _sqlExecutorMock = new Mock<ISqlQueryExecutor>();
    }

    private SourceMachineKeyRepository CreateRepository() => new(
        _sqlExecutorMock.Object,
        Mock.Of<ILogger<SourceMachineKeyRepository>>());

    [Test]
    public void SourceMachineKeyRepository_Should_Implement_ISourceMachineKeyRepository()
    {
        CreateRepository().ShouldBeAssignableTo<ISourceMachineKeyRepository>();
    }

    [Test]
    public async Task EnrollAsync_Should_ReturnEnrolledKey()
    {
        var expected = _fixture.Create<SourceMachineKey>();
        _sqlExecutorMock
            .Setup(e => e.QuerySingleAsync(QuerySourceMachineKeys.EnrollSql, It.IsAny<Action<NpgsqlParameterCollection>>(), It.IsAny<Func<NpgsqlDataReader, SourceMachineKey>>()))
            .ReturnsAsync(expected);

        var result = await CreateRepository().EnrollAsync(_fixture.Create<int>(), SourceMachineKeyPurpose.Recovery, SourceMachineKeyAlgorithm.Ed25519, _fixture.Create<string>());

        result.ShouldBe(expected);
    }

    [Test]
    public void EnrollAsync_Should_Rethrow_When_ExecutorThrows()
    {
        _sqlExecutorMock
            .Setup(e => e.QuerySingleAsync(QuerySourceMachineKeys.EnrollSql, It.IsAny<Action<NpgsqlParameterCollection>>(), It.IsAny<Func<NpgsqlDataReader, SourceMachineKey>>()))
            .ThrowsAsync(new InvalidOperationException("boom"));

        Should.ThrowAsync<InvalidOperationException>(
            () => CreateRepository().EnrollAsync(_fixture.Create<int>(), SourceMachineKeyPurpose.Operational, SourceMachineKeyAlgorithm.Ed25519, _fixture.Create<string>()));
    }

    [Test]
    public async Task GetActiveBySourceMachineIdAsync_Should_ReturnEveryActiveKey()
    {
        var expected = new List<SourceMachineKey>
        {
            _fixture.Build<SourceMachineKey>().With(k => k.KeyPurpose, SourceMachineKeyPurpose.Operational).Create(),
            _fixture.Build<SourceMachineKey>().With(k => k.KeyPurpose, SourceMachineKeyPurpose.Recovery).Create()
        };
        _sqlExecutorMock
            .Setup(e => e.QueryManyAsync(QuerySourceMachineKeys.GetActiveBySourceMachineIdSql, It.IsAny<Action<NpgsqlParameterCollection>>(), It.IsAny<Func<NpgsqlDataReader, SourceMachineKey>>()))
            .ReturnsAsync(expected);

        var result = await CreateRepository().GetActiveBySourceMachineIdAsync(_fixture.Create<int>());

        result.ShouldBe(expected);
    }

    [Test]
    public async Task GetActiveByPublicKeyAsync_Should_ReturnNull_When_KeyIsUnknownOrRevoked()
    {
        _sqlExecutorMock
            .Setup(e => e.QuerySingleAsync(QuerySourceMachineKeys.GetActiveByPublicKeySql, It.IsAny<Action<NpgsqlParameterCollection>>(), It.IsAny<Func<NpgsqlDataReader, SourceMachineKey>>()))
            .ReturnsAsync((SourceMachineKey?)null);

        var result = await CreateRepository().GetActiveByPublicKeyAsync(_fixture.Create<string>());

        result.ShouldBeNull();
    }

    [Test]
    public async Task GetActiveByPublicKeyAsync_Should_ReturnTheEnrollment_When_KeyIsActive()
    {
        var expected = _fixture.Create<SourceMachineKey>();
        _sqlExecutorMock
            .Setup(e => e.QuerySingleAsync(QuerySourceMachineKeys.GetActiveByPublicKeySql, It.IsAny<Action<NpgsqlParameterCollection>>(), It.IsAny<Func<NpgsqlDataReader, SourceMachineKey>>()))
            .ReturnsAsync(expected);

        var result = await CreateRepository().GetActiveByPublicKeyAsync(_fixture.Create<string>());

        result.ShouldBe(expected);
    }

    [Test]
    public async Task RevokeIfActiveAsync_Should_ReturnRevokedKey_When_ItWasStillActive()
    {
        var expected = _fixture.Build<SourceMachineKey>().With(k => k.IsActive, false).Create();
        _sqlExecutorMock
            .Setup(e => e.QuerySingleAsync(QuerySourceMachineKeys.RevokeIfActiveSql, It.IsAny<Action<NpgsqlParameterCollection>>(), It.IsAny<Func<NpgsqlDataReader, SourceMachineKey>>()))
            .ReturnsAsync(expected);

        var result = await CreateRepository().RevokeIfActiveAsync(_fixture.Create<Guid>());

        result.ShouldBe(expected);
    }

    [Test]
    public async Task RevokeIfActiveAsync_Should_ReturnNull_When_AlreadyRevoked()
    {
        _sqlExecutorMock
            .Setup(e => e.QuerySingleAsync(QuerySourceMachineKeys.RevokeIfActiveSql, It.IsAny<Action<NpgsqlParameterCollection>>(), It.IsAny<Func<NpgsqlDataReader, SourceMachineKey>>()))
            .ReturnsAsync((SourceMachineKey?)null);

        var result = await CreateRepository().RevokeIfActiveAsync(_fixture.Create<Guid>());

        result.ShouldBeNull();
    }

    [Test]
    public void RevokeIfActiveAsync_Should_StampRevokedOnAndUpdatedOn_Together()
    {
        NpgsqlParameterCollection? captured = null;
        _sqlExecutorMock
            .Setup(e => e.QuerySingleAsync(QuerySourceMachineKeys.RevokeIfActiveSql, It.IsAny<Action<NpgsqlParameterCollection>>(), It.IsAny<Func<NpgsqlDataReader, SourceMachineKey>>()))
            .Callback<string, Action<NpgsqlParameterCollection>, Func<NpgsqlDataReader, SourceMachineKey>>((_, configure, _) =>
            {
                captured = new NpgsqlCommand().Parameters;
                configure(captured);
            })
            .ReturnsAsync((SourceMachineKey?)null);

        CreateRepository().RevokeIfActiveAsync(Guid.NewGuid()).GetAwaiter().GetResult();

        captured.ShouldNotBeNull();
        captured!["RevokedOn"].Value.ShouldBe(captured["UpdatedOn"].Value);
    }

    [Test]
    public async Task GetActiveByUuidAsync_Should_ReturnTheKey_When_ItIsStillActive()
    {
        var expected = _fixture.Build<SourceMachineKey>().With(k => k.IsActive, true).Create();
        _sqlExecutorMock
            .Setup(e => e.QuerySingleAsync(QuerySourceMachineKeys.GetActiveByUuidSql, It.IsAny<Action<NpgsqlParameterCollection>>(), It.IsAny<Func<NpgsqlDataReader, SourceMachineKey>>()))
            .ReturnsAsync(expected);

        var result = await CreateRepository().GetActiveByUuidAsync(_fixture.Create<Guid>());

        result.ShouldBe(expected);
    }

    [Test]
    public async Task GetActiveByUuidAsync_Should_ReturnNull_When_TheKeyIsRevokedOrUnknown()
    {
        _sqlExecutorMock
            .Setup(e => e.QuerySingleAsync(QuerySourceMachineKeys.GetActiveByUuidSql, It.IsAny<Action<NpgsqlParameterCollection>>(), It.IsAny<Func<NpgsqlDataReader, SourceMachineKey>>()))
            .ReturnsAsync((SourceMachineKey?)null);

        var result = await CreateRepository().GetActiveByUuidAsync(_fixture.Create<Guid>());

        result.ShouldBeNull();
    }
}
