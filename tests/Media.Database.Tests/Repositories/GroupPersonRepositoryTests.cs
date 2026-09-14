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
public class GroupPersonRepositoryTests
{
    private Mock<ISqlQueryExecutor> _sqlExecutorMock = null!;
    private IFixture _fixture = null!;

    [SetUp]
    public void Setup()
    {
        _fixture = AutoMoqFixture.Create();
        _sqlExecutorMock = new Mock<ISqlQueryExecutor>();
    }

    private GroupPersonRepository CreateRepository() => new(
        _sqlExecutorMock.Object,
        new MapGroupPersonResponse(),
        Mock.Of<ILogger<GroupPersonRepository>>());

    private GroupPerson CreateGroupPerson(bool isAdmin = false) => _fixture.Create<GroupPerson>() with { IsAdmin = isAdmin };

    [Test]
    public void GroupPersonRepository_Should_Implement_IGroupPersonRepository()
    {
        CreateRepository().ShouldBeAssignableTo<IGroupPersonRepository>();
    }

    [Test]
    public async Task UpsertAsync_Should_ReturnUpsertedRow()
    {
        var upserted = CreateGroupPerson(isAdmin: true);
        _sqlExecutorMock
            .Setup(e => e.QuerySingleAsync(QueryGroupsPersons.UpsertSql, It.IsAny<Action<NpgsqlParameterCollection>>(), It.IsAny<Func<NpgsqlDataReader, GroupPerson>>()))
            .ReturnsAsync(upserted);

        var result = await CreateRepository().UpsertAsync(upserted.GroupId, upserted.PersonId, true);

        result.ShouldBe(upserted);
    }

    [Test]
    public async Task UpsertAsync_Should_ConfigureParameters()
    {
        Action<NpgsqlParameterCollection>? captured = null;
        var groupPerson = CreateGroupPerson();
        _sqlExecutorMock
            .Setup(e => e.QuerySingleAsync(QueryGroupsPersons.UpsertSql, It.IsAny<Action<NpgsqlParameterCollection>>(), It.IsAny<Func<NpgsqlDataReader, GroupPerson>>()))
            .Callback<string, Action<NpgsqlParameterCollection>, Func<NpgsqlDataReader, GroupPerson>>((_, configure, _) => captured = configure)
            .ReturnsAsync(groupPerson);

        await CreateRepository().UpsertAsync(7, 9, true);

        using var command = new NpgsqlCommand();
        captured!(command.Parameters);
        command.Parameters[pn.GroupId].Value.ShouldBe(7);
        command.Parameters[pn.PersonId].Value.ShouldBe(9);
        command.Parameters[pn.IsAdmin].Value.ShouldBe(true);
    }

    [Test]
    public void UpsertAsync_Should_Rethrow_When_ExecutorThrows()
    {
        _sqlExecutorMock
            .Setup(e => e.QuerySingleAsync(QueryGroupsPersons.UpsertSql, It.IsAny<Action<NpgsqlParameterCollection>>(), It.IsAny<Func<NpgsqlDataReader, GroupPerson>>()))
            .ThrowsAsync(new InvalidOperationException("boom"));

        Should.ThrowAsync<InvalidOperationException>(() => CreateRepository().UpsertAsync(1, 2, false));
    }

    [Test]
    public async Task GetActiveAsync_Should_ReturnRow_When_Active()
    {
        var expected = CreateGroupPerson();
        _sqlExecutorMock
            .Setup(e => e.QuerySingleAsync(QueryGroupsPersons.GetActiveSql, It.IsAny<Action<NpgsqlParameterCollection>>(), It.IsAny<Func<NpgsqlDataReader, GroupPerson>>()))
            .ReturnsAsync(expected);

        var result = await CreateRepository().GetActiveAsync(expected.GroupId, expected.PersonId);

        result.ShouldBe(expected);
    }

    [Test]
    public async Task GetActiveAsync_Should_ReturnNull_When_NoneActive()
    {
        _sqlExecutorMock
            .Setup(e => e.QuerySingleAsync(QueryGroupsPersons.GetActiveSql, It.IsAny<Action<NpgsqlParameterCollection>>(), It.IsAny<Func<NpgsqlDataReader, GroupPerson>>()))
            .ReturnsAsync((GroupPerson?)null);

        var result = await CreateRepository().GetActiveAsync(1, 2);

        result.ShouldBeNull();
    }

    [Test]
    public async Task DeactivateAsync_Should_ReturnDeactivatedRow_When_Found()
    {
        var deactivated = CreateGroupPerson();
        _sqlExecutorMock
            .Setup(e => e.QuerySingleAsync(QueryGroupsPersons.DeactivateSql, It.IsAny<Action<NpgsqlParameterCollection>>(), It.IsAny<Func<NpgsqlDataReader, GroupPerson>>()))
            .ReturnsAsync(deactivated);

        var result = await CreateRepository().DeactivateAsync(deactivated.GroupId, deactivated.PersonId);

        result.ShouldBe(deactivated);
    }

    [Test]
    public async Task CountActiveAdminsAsync_Should_ReturnCount()
    {
        _sqlExecutorMock
            .Setup(e => e.QuerySingleValueAsync(QueryGroupsPersons.CountActiveAdminsSql, It.IsAny<Action<NpgsqlParameterCollection>>(), It.IsAny<Func<NpgsqlDataReader, long>>()))
            .ReturnsAsync(3L);

        var result = await CreateRepository().CountActiveAdminsAsync(5);

        result.ShouldBe(3);
    }

    [Test]
    public async Task CountActiveAdminsAsync_Should_ReturnZero_When_NoRowReturned()
    {
        _sqlExecutorMock
            .Setup(e => e.QuerySingleValueAsync(QueryGroupsPersons.CountActiveAdminsSql, It.IsAny<Action<NpgsqlParameterCollection>>(), It.IsAny<Func<NpgsqlDataReader, long>>()))
            .ReturnsAsync((long?)null);

        var result = await CreateRepository().CountActiveAdminsAsync(5);

        result.ShouldBe(0);
    }

    [Test]
    public void CountActiveAdminsAsync_Should_Rethrow_When_ExecutorThrows()
    {
        _sqlExecutorMock
            .Setup(e => e.QuerySingleValueAsync(QueryGroupsPersons.CountActiveAdminsSql, It.IsAny<Action<NpgsqlParameterCollection>>(), It.IsAny<Func<NpgsqlDataReader, long>>()))
            .ThrowsAsync(new InvalidOperationException("boom"));

        Should.ThrowAsync<InvalidOperationException>(() => CreateRepository().CountActiveAdminsAsync(5));
    }
}
