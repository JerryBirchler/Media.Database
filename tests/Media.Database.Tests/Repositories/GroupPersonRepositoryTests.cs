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
using System.Collections.Generic;
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

    [Test]
    public async Task GetGroupIdentifiersByPersonIdAsync_Should_ReturnIdentifiers_From_Executor()
    {
        var expected = new List<(int GroupId, string Name)> { (GroupId: 1, Name: "a"), (GroupId: 2, Name: "b") };
        _sqlExecutorMock
            .Setup(e => e.QueryManyAsync(QueryGroupsPersons.GetGroupIdentifiersByPersonIdSql, It.IsAny<Action<NpgsqlParameterCollection>>(), It.IsAny<Func<NpgsqlDataReader, (int GroupId, string Name)>>()))
            .ReturnsAsync(expected);

        var result = await CreateRepository().GetGroupIdentifiersByPersonIdAsync(3, next: null, limit: 5);

        result.ShouldBe(expected);
    }

    [Test]
    public async Task GetGroupIdentifiersByPersonIdAsync_Should_ConfigureParameters()
    {
        Action<NpgsqlParameterCollection>? captured = null;
        _sqlExecutorMock
            .Setup(e => e.QueryManyAsync(QueryGroupsPersons.GetGroupIdentifiersByPersonIdSql, It.IsAny<Action<NpgsqlParameterCollection>>(), It.IsAny<Func<NpgsqlDataReader, (int GroupId, string Name)>>()))
            .Callback<string, Action<NpgsqlParameterCollection>, Func<NpgsqlDataReader, (int GroupId, string Name)>>((_, configure, _) => captured = configure)
            .ReturnsAsync([]);

        await CreateRepository().GetGroupIdentifiersByPersonIdAsync(3, next: (GroupId: 7, Name: "Group A"), limit: 5);

        using var command = new NpgsqlCommand();
        captured!(command.Parameters);
        command.Parameters[pn.PersonId].Value.ShouldBe(3);
        command.Parameters[pn.Name].Value.ShouldBe("Group A");
        command.Parameters[pn.GroupId].Value.ShouldBe(7);
        command.Parameters[pn.Limit].Value.ShouldBe(5);
    }

    [Test]
    public async Task GetGroupIdentifiersByPersonIdAsync_Should_ConfigureCursorAsDbNull_When_FirstPage()
    {
        Action<NpgsqlParameterCollection>? captured = null;
        _sqlExecutorMock
            .Setup(e => e.QueryManyAsync(QueryGroupsPersons.GetGroupIdentifiersByPersonIdSql, It.IsAny<Action<NpgsqlParameterCollection>>(), It.IsAny<Func<NpgsqlDataReader, (int GroupId, string Name)>>()))
            .Callback<string, Action<NpgsqlParameterCollection>, Func<NpgsqlDataReader, (int GroupId, string Name)>>((_, configure, _) => captured = configure)
            .ReturnsAsync([]);

        await CreateRepository().GetGroupIdentifiersByPersonIdAsync(3, next: null, limit: 5);

        using var command = new NpgsqlCommand();
        captured!(command.Parameters);
        command.Parameters[pn.Name].Value.ShouldBe(DBNull.Value);
        command.Parameters[pn.GroupId].Value.ShouldBe(DBNull.Value);
    }

    [Test]
    public void GetGroupIdentifiersByPersonIdAsync_Should_Rethrow_When_ExecutorThrows()
    {
        _sqlExecutorMock
            .Setup(e => e.QueryManyAsync(QueryGroupsPersons.GetGroupIdentifiersByPersonIdSql, It.IsAny<Action<NpgsqlParameterCollection>>(), It.IsAny<Func<NpgsqlDataReader, (int GroupId, string Name)>>()))
            .ThrowsAsync(new InvalidOperationException("boom"));

        Should.ThrowAsync<InvalidOperationException>(() => CreateRepository().GetGroupIdentifiersByPersonIdAsync(3, null, 5));
    }

    [Test]
    public async Task GetPersonIdentifiersByGroupIdAsync_Should_ReturnIdentifiers_From_Executor()
    {
        var expected = new List<PersonIdentifier> { _fixture.Create<PersonIdentifier>() };
        _sqlExecutorMock
            .Setup(e => e.QueryManyAsync(QueryGroupsPersons.GetPersonIdentifiersByGroupIdSql, It.IsAny<Action<NpgsqlParameterCollection>>(), It.IsAny<Func<NpgsqlDataReader, PersonIdentifier>>()))
            .ReturnsAsync(expected);

        var result = await CreateRepository().GetPersonIdentifiersByGroupIdAsync(3, next: null, limit: 5);

        result.ShouldBe(expected);
    }

    [Test]
    public async Task GetPersonIdentifiersByGroupIdAsync_Should_ConfigureParameters()
    {
        Action<NpgsqlParameterCollection>? captured = null;
        var personUuid = Guid.NewGuid();
        var next = new PersonIdentifier { LastName = "Doe", FirstName = "Jane", PersonUuid = personUuid };
        _sqlExecutorMock
            .Setup(e => e.QueryManyAsync(QueryGroupsPersons.GetPersonIdentifiersByGroupIdSql, It.IsAny<Action<NpgsqlParameterCollection>>(), It.IsAny<Func<NpgsqlDataReader, PersonIdentifier>>()))
            .Callback<string, Action<NpgsqlParameterCollection>, Func<NpgsqlDataReader, PersonIdentifier>>((_, configure, _) => captured = configure)
            .ReturnsAsync([]);

        await CreateRepository().GetPersonIdentifiersByGroupIdAsync(3, next, limit: 5);

        using var command = new NpgsqlCommand();
        captured!(command.Parameters);
        command.Parameters[pn.GroupId].Value.ShouldBe(3);
        command.Parameters[pn.LastName].Value.ShouldBe("Doe");
        command.Parameters[pn.FirstName].Value.ShouldBe("Jane");
        command.Parameters[pn.PersonUuid].Value.ShouldBe(personUuid);
        command.Parameters[pn.Limit].Value.ShouldBe(5);
    }

    [Test]
    public async Task GetPersonIdentifiersByGroupIdAsync_Should_ConfigureCursorAsDbNull_When_FirstPage()
    {
        Action<NpgsqlParameterCollection>? captured = null;
        _sqlExecutorMock
            .Setup(e => e.QueryManyAsync(QueryGroupsPersons.GetPersonIdentifiersByGroupIdSql, It.IsAny<Action<NpgsqlParameterCollection>>(), It.IsAny<Func<NpgsqlDataReader, PersonIdentifier>>()))
            .Callback<string, Action<NpgsqlParameterCollection>, Func<NpgsqlDataReader, PersonIdentifier>>((_, configure, _) => captured = configure)
            .ReturnsAsync([]);

        await CreateRepository().GetPersonIdentifiersByGroupIdAsync(3, next: null, limit: 5);

        using var command = new NpgsqlCommand();
        captured!(command.Parameters);
        command.Parameters[pn.LastName].Value.ShouldBe(DBNull.Value);
        command.Parameters[pn.FirstName].Value.ShouldBe(DBNull.Value);
        command.Parameters[pn.PersonUuid].Value.ShouldBe(DBNull.Value);
    }

    [Test]
    public void GetPersonIdentifiersByGroupIdAsync_Should_Rethrow_When_ExecutorThrows()
    {
        _sqlExecutorMock
            .Setup(e => e.QueryManyAsync(QueryGroupsPersons.GetPersonIdentifiersByGroupIdSql, It.IsAny<Action<NpgsqlParameterCollection>>(), It.IsAny<Func<NpgsqlDataReader, PersonIdentifier>>()))
            .ThrowsAsync(new InvalidOperationException("boom"));

        Should.ThrowAsync<InvalidOperationException>(() => CreateRepository().GetPersonIdentifiersByGroupIdAsync(3, null, 5));
    }

    [Test]
    public async Task GetAccessByGroupPersonUuidAsync_Should_ReturnAccess_When_ExecutorFindsMatch()
    {
        var expected = _fixture.Create<GroupAccess>();
        _sqlExecutorMock
            .Setup(e => e.QuerySingleAsync(QueryGroupsPersons.GetAccessByGroupPersonUuidSql, It.IsAny<Action<NpgsqlParameterCollection>>(), It.IsAny<Func<NpgsqlDataReader, GroupAccess>>()))
            .ReturnsAsync(expected);

        var result = await CreateRepository().GetAccessByGroupPersonUuidAsync(Guid.NewGuid());

        result.ShouldBe(expected);
    }

    [Test]
    public async Task GetAccessByGroupPersonUuidAsync_Should_ReturnNull_When_ExecutorFindsNoMatch()
    {
        _sqlExecutorMock
            .Setup(e => e.QuerySingleAsync(QueryGroupsPersons.GetAccessByGroupPersonUuidSql, It.IsAny<Action<NpgsqlParameterCollection>>(), It.IsAny<Func<NpgsqlDataReader, GroupAccess>>()))
            .ReturnsAsync((GroupAccess?)null);

        var result = await CreateRepository().GetAccessByGroupPersonUuidAsync(Guid.NewGuid());

        result.ShouldBeNull();
    }

    [Test]
    public async Task GetAccessByGroupPersonUuidAsync_Should_ConfigureGroupPersonUuidParameter()
    {
        Action<NpgsqlParameterCollection>? captured = null;
        _sqlExecutorMock
            .Setup(e => e.QuerySingleAsync(QueryGroupsPersons.GetAccessByGroupPersonUuidSql, It.IsAny<Action<NpgsqlParameterCollection>>(), It.IsAny<Func<NpgsqlDataReader, GroupAccess>>()))
            .Callback<string, Action<NpgsqlParameterCollection>, Func<NpgsqlDataReader, GroupAccess>>((_, configure, _) => captured = configure)
            .ReturnsAsync((GroupAccess?)null);
        var uuid = Guid.NewGuid();

        await CreateRepository().GetAccessByGroupPersonUuidAsync(uuid);

        using var command = new NpgsqlCommand();
        captured!(command.Parameters);
        command.Parameters[pn.GroupPersonUuid].Value.ShouldBe(uuid);
    }

    [Test]
    public void GetAccessByGroupPersonUuidAsync_Should_Rethrow_When_ExecutorThrows()
    {
        _sqlExecutorMock
            .Setup(e => e.QuerySingleAsync(QueryGroupsPersons.GetAccessByGroupPersonUuidSql, It.IsAny<Action<NpgsqlParameterCollection>>(), It.IsAny<Func<NpgsqlDataReader, GroupAccess>>()))
            .ThrowsAsync(new InvalidOperationException("boom"));

        Should.ThrowAsync<InvalidOperationException>(() => CreateRepository().GetAccessByGroupPersonUuidAsync(Guid.NewGuid()));
    }
}
