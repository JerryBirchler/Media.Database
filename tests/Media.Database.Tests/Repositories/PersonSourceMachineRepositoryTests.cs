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
public class PersonSourceMachineRepositoryTests
{
    private Mock<ISqlQueryExecutor> _sqlExecutorMock = null!;
    private IFixture _fixture = null!;

    [SetUp]
    public void Setup()
    {
        _fixture = AutoMoqFixture.Create();
        _sqlExecutorMock = new Mock<ISqlQueryExecutor>();
    }

    private PersonSourceMachineRepository CreateRepository() => new(
        _sqlExecutorMock.Object,
        new MapPersonSourceMachineResponse(),
        Mock.Of<ILogger<PersonSourceMachineRepository>>());

    private PersonSourceMachine CreatePersonSourceMachine() => _fixture.Create<PersonSourceMachine>();

    [Test]
    public void PersonSourceMachineRepository_Should_Implement_IPersonSourceMachineRepository()
    {
        CreateRepository().ShouldBeAssignableTo<IPersonSourceMachineRepository>();
    }

    [Test]
    public async Task GetActiveAsync_Should_ReturnRow_When_Active()
    {
        var expected = CreatePersonSourceMachine();
        _sqlExecutorMock
            .Setup(e => e.QuerySingleAsync(QueryPersonsSourceMachines.GetActiveSql, It.IsAny<Action<NpgsqlParameterCollection>>(), It.IsAny<Func<NpgsqlDataReader, PersonSourceMachine>>()))
            .ReturnsAsync(expected);

        var result = await CreateRepository().GetActiveAsync(expected.PersonId, expected.SourceMachineId);

        result.ShouldBe(expected);
    }

    [Test]
    public async Task GetActiveAsync_Should_ReturnNull_When_NoneActive()
    {
        _sqlExecutorMock
            .Setup(e => e.QuerySingleAsync(QueryPersonsSourceMachines.GetActiveSql, It.IsAny<Action<NpgsqlParameterCollection>>(), It.IsAny<Func<NpgsqlDataReader, PersonSourceMachine>>()))
            .ReturnsAsync((PersonSourceMachine?)null);

        var result = await CreateRepository().GetActiveAsync(1, 2);

        result.ShouldBeNull();
    }

    [Test]
    public async Task GetActiveAsync_Should_ConfigureParameters()
    {
        Action<NpgsqlParameterCollection>? captured = null;
        _sqlExecutorMock
            .Setup(e => e.QuerySingleAsync(QueryPersonsSourceMachines.GetActiveSql, It.IsAny<Action<NpgsqlParameterCollection>>(), It.IsAny<Func<NpgsqlDataReader, PersonSourceMachine>>()))
            .Callback<string, Action<NpgsqlParameterCollection>, Func<NpgsqlDataReader, PersonSourceMachine>>((_, configure, _) => captured = configure)
            .ReturnsAsync((PersonSourceMachine?)null);

        await CreateRepository().GetActiveAsync(11, 22);

        using var command = new NpgsqlCommand();
        captured!(command.Parameters);
        command.Parameters[pn.PersonId].Value.ShouldBe(11);
        command.Parameters[pn.SourceMachineId].Value.ShouldBe(22);
    }

    [Test]
    public async Task ListActiveByPersonAsync_Should_ReturnAssociations()
    {
        var expected = new System.Collections.Generic.List<PersonSourceMachine> { CreatePersonSourceMachine(), CreatePersonSourceMachine() };
        _sqlExecutorMock
            .Setup(e => e.QueryManyAsync(QueryPersonsSourceMachines.ListActiveByPersonSql, It.IsAny<Action<NpgsqlParameterCollection>>(), It.IsAny<Func<NpgsqlDataReader, PersonSourceMachine>>()))
            .ReturnsAsync(expected);

        var result = await CreateRepository().ListActiveByPersonAsync(1);

        result.ShouldBe(expected);
    }

    [Test]
    public async Task CreateAsync_Should_ReturnCreatedRow()
    {
        var created = CreatePersonSourceMachine();
        _sqlExecutorMock
            .Setup(e => e.QuerySingleAsync(QueryPersonsSourceMachines.AddSql, It.IsAny<Action<NpgsqlParameterCollection>>(), It.IsAny<Func<NpgsqlDataReader, PersonSourceMachine>>()))
            .ReturnsAsync(created);

        var result = await CreateRepository().CreateAsync(created.PersonId, created.SourceMachineId);

        result.ShouldBe(created);
    }

    [Test]
    public void CreateAsync_Should_Rethrow_When_ExecutorThrows()
    {
        _sqlExecutorMock
            .Setup(e => e.QuerySingleAsync(QueryPersonsSourceMachines.AddSql, It.IsAny<Action<NpgsqlParameterCollection>>(), It.IsAny<Func<NpgsqlDataReader, PersonSourceMachine>>()))
            .ThrowsAsync(new InvalidOperationException("boom"));

        Should.ThrowAsync<InvalidOperationException>(() => CreateRepository().CreateAsync(1, 2));
    }
}
