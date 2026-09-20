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
public class GroupShellRepositoryTests
{
    private Mock<ISqlQueryExecutor> _sqlExecutorMock = null!;
    private IFixture _fixture = null!;

    [SetUp]
    public void Setup()
    {
        _fixture = AutoMoqFixture.Create();
        _sqlExecutorMock = new Mock<ISqlQueryExecutor>();
    }

    private GroupShellRepository CreateRepository() => new(
        _sqlExecutorMock.Object,
        Mock.Of<ILogger<GroupShellRepository>>());

    [Test]
    public void GroupShellRepository_Should_Implement_IGroupShellRepository()
    {
        CreateRepository().ShouldBeAssignableTo<IGroupShellRepository>();
    }

    [Test]
    public async Task CreateAsync_Should_ReturnInsertedShell()
    {
        var expected = _fixture.Create<GroupShell>();
        _sqlExecutorMock
            .Setup(e => e.QuerySingleAsync(QueryGroupShell.InsertSql, It.IsAny<Action<NpgsqlParameterCollection>>(), It.IsAny<Func<NpgsqlDataReader, GroupShell>>()))
            .ReturnsAsync(expected);

        var result = await CreateRepository().CreateAsync();

        result.ShouldBe(expected);
    }

    [Test]
    public void CreateAsync_Should_Rethrow_When_ExecutorThrows()
    {
        _sqlExecutorMock
            .Setup(e => e.QuerySingleAsync(QueryGroupShell.InsertSql, It.IsAny<Action<NpgsqlParameterCollection>>(), It.IsAny<Func<NpgsqlDataReader, GroupShell>>()))
            .ThrowsAsync(new InvalidOperationException("boom"));

        Should.ThrowAsync<InvalidOperationException>(() => CreateRepository().CreateAsync());
    }

    [Test]
    public async Task GetByIdAsync_Should_ReturnShell_When_ExecutorFindsMatch()
    {
        var expected = _fixture.Create<GroupShell>();
        _sqlExecutorMock
            .Setup(e => e.QuerySingleAsync(QueryGroupShell.GetByIdSql, It.IsAny<Action<NpgsqlParameterCollection>>(), It.IsAny<Func<NpgsqlDataReader, GroupShell>>()))
            .ReturnsAsync(expected);

        var result = await CreateRepository().GetByIdAsync(expected.GroupShellId);

        result.ShouldBe(expected);
    }

    [Test]
    public async Task GetByIdAsync_Should_ReturnNull_When_ExecutorFindsNoMatch()
    {
        _sqlExecutorMock
            .Setup(e => e.QuerySingleAsync(QueryGroupShell.GetByIdSql, It.IsAny<Action<NpgsqlParameterCollection>>(), It.IsAny<Func<NpgsqlDataReader, GroupShell>>()))
            .ReturnsAsync((GroupShell?)null);

        var result = await CreateRepository().GetByIdAsync(1);

        result.ShouldBeNull();
    }

    [Test]
    public async Task GetByIdAsync_Should_ConfigureIdParameter()
    {
        Action<NpgsqlParameterCollection>? captured = null;
        _sqlExecutorMock
            .Setup(e => e.QuerySingleAsync(QueryGroupShell.GetByIdSql, It.IsAny<Action<NpgsqlParameterCollection>>(), It.IsAny<Func<NpgsqlDataReader, GroupShell>>()))
            .Callback<string, Action<NpgsqlParameterCollection>, Func<NpgsqlDataReader, GroupShell>>((_, configure, _) => captured = configure)
            .ReturnsAsync((GroupShell?)null);

        await CreateRepository().GetByIdAsync(7);

        using var command = new NpgsqlCommand();
        captured!(command.Parameters);
        command.Parameters[pn.Id].Value.ShouldBe(7);
    }

    [Test]
    public void GetByIdAsync_Should_Rethrow_When_ExecutorThrows()
    {
        _sqlExecutorMock
            .Setup(e => e.QuerySingleAsync(QueryGroupShell.GetByIdSql, It.IsAny<Action<NpgsqlParameterCollection>>(), It.IsAny<Func<NpgsqlDataReader, GroupShell>>()))
            .ThrowsAsync(new InvalidOperationException("boom"));

        Should.ThrowAsync<InvalidOperationException>(() => CreateRepository().GetByIdAsync(1));
    }

    [Test]
    public async Task PromoteIfUnpromotedAsync_Should_ReturnPromotedShell_When_ExecutorFindsMatch()
    {
        var expected = _fixture.Create<GroupShell>();
        _sqlExecutorMock
            .Setup(e => e.QuerySingleAsync(QueryGroupShell.PromoteIfUnpromotedSql, It.IsAny<Action<NpgsqlParameterCollection>>(), It.IsAny<Func<NpgsqlDataReader, GroupShell>>()))
            .ReturnsAsync(expected);

        var result = await CreateRepository().PromoteIfUnpromotedAsync(1, 5);

        result.ShouldBe(expected);
    }

    [Test]
    public async Task PromoteIfUnpromotedAsync_Should_ReturnNull_When_AlreadyPromotedOrMissing()
    {
        _sqlExecutorMock
            .Setup(e => e.QuerySingleAsync(QueryGroupShell.PromoteIfUnpromotedSql, It.IsAny<Action<NpgsqlParameterCollection>>(), It.IsAny<Func<NpgsqlDataReader, GroupShell>>()))
            .ReturnsAsync((GroupShell?)null);

        var result = await CreateRepository().PromoteIfUnpromotedAsync(1, 5);

        result.ShouldBeNull();
    }

    [Test]
    public async Task PromoteIfUnpromotedAsync_Should_ConfigureParameters()
    {
        Action<NpgsqlParameterCollection>? captured = null;
        _sqlExecutorMock
            .Setup(e => e.QuerySingleAsync(QueryGroupShell.PromoteIfUnpromotedSql, It.IsAny<Action<NpgsqlParameterCollection>>(), It.IsAny<Func<NpgsqlDataReader, GroupShell>>()))
            .Callback<string, Action<NpgsqlParameterCollection>, Func<NpgsqlDataReader, GroupShell>>((_, configure, _) => captured = configure)
            .ReturnsAsync((GroupShell?)null);

        await CreateRepository().PromoteIfUnpromotedAsync(1, 5);

        using var command = new NpgsqlCommand();
        captured!(command.Parameters);
        command.Parameters[pn.Id].Value.ShouldBe(1);
        command.Parameters[pn.GroupId].Value.ShouldBe(5);
    }

    [Test]
    public void PromoteIfUnpromotedAsync_Should_Rethrow_When_ExecutorThrows()
    {
        _sqlExecutorMock
            .Setup(e => e.QuerySingleAsync(QueryGroupShell.PromoteIfUnpromotedSql, It.IsAny<Action<NpgsqlParameterCollection>>(), It.IsAny<Func<NpgsqlDataReader, GroupShell>>()))
            .ThrowsAsync(new InvalidOperationException("boom"));

        Should.ThrowAsync<InvalidOperationException>(() => CreateRepository().PromoteIfUnpromotedAsync(1, 5));
    }
}
