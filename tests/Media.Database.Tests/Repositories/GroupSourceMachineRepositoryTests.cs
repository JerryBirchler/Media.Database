#nullable enable
using AutoFixture;
using Media.Database.Mappers;
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
public class GroupSourceMachineRepositoryTests
{
    private Mock<ISqlQueryExecutor> _sqlExecutorMock = null!;
    private IFixture _fixture = null!;

    [SetUp]
    public void Setup()
    {
        _fixture = AutoMoqFixture.Create();
        _sqlExecutorMock = new Mock<ISqlQueryExecutor>();
    }

    private GroupSourceMachineRepository CreateRepository() => new(
        _sqlExecutorMock.Object,
        new MapGroupSourceMachineResponse(),
        Mock.Of<ILogger<GroupSourceMachineRepository>>());

    private GroupSourceMachine CreateGroupSourceMachine() => _fixture.Create<GroupSourceMachine>();

    [Test]
    public void GroupSourceMachineRepository_Should_Implement_IGroupSourceMachineRepository()
    {
        CreateRepository().ShouldBeAssignableTo<IGroupSourceMachineRepository>();
    }

    [Test]
    public async Task UpsertAsync_Should_ReturnUpsertedRow()
    {
        var upserted = CreateGroupSourceMachine();
        _sqlExecutorMock
            .Setup(e => e.QuerySingleAsync(QueryGroupsSourceMachines.UpsertSql, It.IsAny<Action<NpgsqlParameterCollection>>(), It.IsAny<Func<NpgsqlDataReader, GroupSourceMachine>>()))
            .ReturnsAsync(upserted);

        var result = await CreateRepository().UpsertAsync(upserted.GroupId, upserted.SourceMachineId);

        result.ShouldBe(upserted);
    }

    [Test]
    public async Task UpsertAsync_Should_ConfigureParameters()
    {
        Action<NpgsqlParameterCollection>? captured = null;
        var groupSourceMachine = CreateGroupSourceMachine();
        _sqlExecutorMock
            .Setup(e => e.QuerySingleAsync(QueryGroupsSourceMachines.UpsertSql, It.IsAny<Action<NpgsqlParameterCollection>>(), It.IsAny<Func<NpgsqlDataReader, GroupSourceMachine>>()))
            .Callback<string, Action<NpgsqlParameterCollection>, Func<NpgsqlDataReader, GroupSourceMachine>>((_, configure, _) => captured = configure)
            .ReturnsAsync(groupSourceMachine);

        await CreateRepository().UpsertAsync(4, 8);

        using var command = new NpgsqlCommand();
        captured!(command.Parameters);
        command.Parameters[pn.GroupId].Value.ShouldBe(4);
        command.Parameters[pn.SourceMachineId].Value.ShouldBe(8);
    }

    [Test]
    public void UpsertAsync_Should_Rethrow_When_ExecutorThrows()
    {
        _sqlExecutorMock
            .Setup(e => e.QuerySingleAsync(QueryGroupsSourceMachines.UpsertSql, It.IsAny<Action<NpgsqlParameterCollection>>(), It.IsAny<Func<NpgsqlDataReader, GroupSourceMachine>>()))
            .ThrowsAsync(new InvalidOperationException("boom"));

        Should.ThrowAsync<InvalidOperationException>(() => CreateRepository().UpsertAsync(1, 2));
    }

    [Test]
    public async Task DeactivateAsync_Should_ReturnDeactivatedRow_When_Found()
    {
        var deactivated = CreateGroupSourceMachine();
        _sqlExecutorMock
            .Setup(e => e.QuerySingleAsync(QueryGroupsSourceMachines.DeactivateSql, It.IsAny<Action<NpgsqlParameterCollection>>(), It.IsAny<Func<NpgsqlDataReader, GroupSourceMachine>>()))
            .ReturnsAsync(deactivated);

        var result = await CreateRepository().DeactivateAsync(deactivated.GroupId, deactivated.SourceMachineId);

        result.ShouldBe(deactivated);
    }

    [Test]
    public async Task DeactivateAsync_Should_ReturnNull_When_NoneActive()
    {
        _sqlExecutorMock
            .Setup(e => e.QuerySingleAsync(QueryGroupsSourceMachines.DeactivateSql, It.IsAny<Action<NpgsqlParameterCollection>>(), It.IsAny<Func<NpgsqlDataReader, GroupSourceMachine>>()))
            .ReturnsAsync((GroupSourceMachine?)null);

        var result = await CreateRepository().DeactivateAsync(1, 2);

        result.ShouldBeNull();
    }

    [Test]
    public void DeactivateAsync_Should_Rethrow_When_ExecutorThrows()
    {
        _sqlExecutorMock
            .Setup(e => e.QuerySingleAsync(QueryGroupsSourceMachines.DeactivateSql, It.IsAny<Action<NpgsqlParameterCollection>>(), It.IsAny<Func<NpgsqlDataReader, GroupSourceMachine>>()))
            .ThrowsAsync(new InvalidOperationException("boom"));

        Should.ThrowAsync<InvalidOperationException>(() => CreateRepository().DeactivateAsync(1, 2));
    }
}
