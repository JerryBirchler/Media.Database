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

[TestFixture]
public class GroupEncryptionKeyRepositoryTests
{
    private Mock<ISqlQueryExecutor> _sqlExecutorMock = null!;
    private IFixture _fixture = null!;

    [SetUp]
    public void Setup()
    {
        _fixture = AutoMoqFixture.Create();
        _sqlExecutorMock = new Mock<ISqlQueryExecutor>();
    }

    private GroupEncryptionKeyRepository CreateRepository() => new(
        _sqlExecutorMock.Object,
        Mock.Of<ILogger<GroupEncryptionKeyRepository>>());

    [Test]
    public void GroupEncryptionKeyRepository_Should_Implement_IGroupEncryptionKeyRepository()
    {
        CreateRepository().ShouldBeAssignableTo<IGroupEncryptionKeyRepository>();
    }

    [Test]
    public async Task CreateAsync_Should_ReturnInsertedKey()
    {
        var expected = _fixture.Create<GroupEncryptionKey>();
        _sqlExecutorMock
            .Setup(e => e.QuerySingleAsync(QueryGroupEncryptionKeys.InsertSql, It.IsAny<Action<NpgsqlParameterCollection>>(), It.IsAny<Func<NpgsqlDataReader, GroupEncryptionKey>>()))
            .ReturnsAsync(expected);

        var result = await CreateRepository().CreateAsync(3, EncryptionDataCategory.UuidOrchestration, "wrapped");

        result.ShouldBe(expected);
    }

    [Test]
    public async Task CreateAsync_Should_ConfigureParameters()
    {
        Action<NpgsqlParameterCollection>? captured = null;
        var expected = _fixture.Create<GroupEncryptionKey>();
        _sqlExecutorMock
            .Setup(e => e.QuerySingleAsync(QueryGroupEncryptionKeys.InsertSql, It.IsAny<Action<NpgsqlParameterCollection>>(), It.IsAny<Func<NpgsqlDataReader, GroupEncryptionKey>>()))
            .Callback<string, Action<NpgsqlParameterCollection>, Func<NpgsqlDataReader, GroupEncryptionKey>>((_, configure, _) => captured = configure)
            .ReturnsAsync(expected);

        await CreateRepository().CreateAsync(3, EncryptionDataCategory.PiiMetadata, "wrapped-dek-value");

        using var command = new NpgsqlCommand();
        captured!(command.Parameters);
        command.Parameters[pn.GroupShellId].Value.ShouldBe(3);
        command.Parameters[pn.DataCategory].Value.ShouldBe((int)EncryptionDataCategory.PiiMetadata);
        command.Parameters[pn.WrappedDek].Value.ShouldBe("wrapped-dek-value");
    }

    [Test]
    public void CreateAsync_Should_Rethrow_When_ExecutorThrows()
    {
        _sqlExecutorMock
            .Setup(e => e.QuerySingleAsync(QueryGroupEncryptionKeys.InsertSql, It.IsAny<Action<NpgsqlParameterCollection>>(), It.IsAny<Func<NpgsqlDataReader, GroupEncryptionKey>>()))
            .ThrowsAsync(new InvalidOperationException("boom"));

        Should.ThrowAsync<InvalidOperationException>(() => CreateRepository().CreateAsync(3, EncryptionDataCategory.PiiMetadata, "wrapped"));
    }

    [Test]
    public async Task GetActiveAsync_Should_ReturnKey_When_ExecutorFindsMatch()
    {
        var expected = _fixture.Create<GroupEncryptionKey>();
        _sqlExecutorMock
            .Setup(e => e.QuerySingleAsync(QueryGroupEncryptionKeys.GetActiveSql, It.IsAny<Action<NpgsqlParameterCollection>>(), It.IsAny<Func<NpgsqlDataReader, GroupEncryptionKey>>()))
            .ReturnsAsync(expected);

        var result = await CreateRepository().GetActiveAsync(3, EncryptionDataCategory.UuidOrchestration);

        result.ShouldBe(expected);
    }

    [Test]
    public async Task GetActiveAsync_Should_ReturnNull_When_ExecutorFindsNoMatch()
    {
        _sqlExecutorMock
            .Setup(e => e.QuerySingleAsync(QueryGroupEncryptionKeys.GetActiveSql, It.IsAny<Action<NpgsqlParameterCollection>>(), It.IsAny<Func<NpgsqlDataReader, GroupEncryptionKey>>()))
            .ReturnsAsync((GroupEncryptionKey?)null);

        var result = await CreateRepository().GetActiveAsync(3, EncryptionDataCategory.UuidOrchestration);

        result.ShouldBeNull();
    }

    [Test]
    public async Task GetActiveAsync_Should_ConfigureParameters()
    {
        Action<NpgsqlParameterCollection>? captured = null;
        _sqlExecutorMock
            .Setup(e => e.QuerySingleAsync(QueryGroupEncryptionKeys.GetActiveSql, It.IsAny<Action<NpgsqlParameterCollection>>(), It.IsAny<Func<NpgsqlDataReader, GroupEncryptionKey>>()))
            .Callback<string, Action<NpgsqlParameterCollection>, Func<NpgsqlDataReader, GroupEncryptionKey>>((_, configure, _) => captured = configure)
            .ReturnsAsync((GroupEncryptionKey?)null);

        await CreateRepository().GetActiveAsync(9, EncryptionDataCategory.PiiMetadata);

        using var command = new NpgsqlCommand();
        captured!(command.Parameters);
        command.Parameters[pn.GroupShellId].Value.ShouldBe(9);
        command.Parameters[pn.DataCategory].Value.ShouldBe((int)EncryptionDataCategory.PiiMetadata);
    }

    [Test]
    public void GetActiveAsync_Should_Rethrow_When_ExecutorThrows()
    {
        _sqlExecutorMock
            .Setup(e => e.QuerySingleAsync(QueryGroupEncryptionKeys.GetActiveSql, It.IsAny<Action<NpgsqlParameterCollection>>(), It.IsAny<Func<NpgsqlDataReader, GroupEncryptionKey>>()))
            .ThrowsAsync(new InvalidOperationException("boom"));

        Should.ThrowAsync<InvalidOperationException>(() => CreateRepository().GetActiveAsync(3, EncryptionDataCategory.PiiMetadata));
    }

    [Test]
    public async Task DeactivateAllAsync_Should_ReturnHowManyKeysWereDeactivated()
    {
        _sqlExecutorMock
            .Setup(e => e.ExecuteAsync(QueryGroupEncryptionKeys.DeactivateAllForShellSql, It.IsAny<Action<NpgsqlParameterCollection>>()))
            .ReturnsAsync(2);

        var result = await CreateRepository().DeactivateAllAsync(_fixture.Create<int>());

        result.ShouldBe(2);
    }

    [Test]
    public async Task DeactivateAllAsync_Should_ReturnZero_When_NothingWasActive()
    {
        _sqlExecutorMock
            .Setup(e => e.ExecuteAsync(QueryGroupEncryptionKeys.DeactivateAllForShellSql, It.IsAny<Action<NpgsqlParameterCollection>>()))
            .ReturnsAsync(0);

        var result = await CreateRepository().DeactivateAllAsync(_fixture.Create<int>());

        result.ShouldBe(0);
    }

    [Test]
    public void DeactivateAllAsync_Should_Rethrow_When_ExecutorThrows()
    {
        _sqlExecutorMock
            .Setup(e => e.ExecuteAsync(QueryGroupEncryptionKeys.DeactivateAllForShellSql, It.IsAny<Action<NpgsqlParameterCollection>>()))
            .ThrowsAsync(new InvalidOperationException("boom"));

        Should.ThrowAsync<InvalidOperationException>(() => CreateRepository().DeactivateAllAsync(_fixture.Create<int>()));
    }
}
