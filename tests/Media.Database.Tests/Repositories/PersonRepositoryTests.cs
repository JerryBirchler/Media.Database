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

/// <summary>
/// Covers PersonRepository's public API against a mocked ISqlQueryExecutor, following the same
/// pattern as RegistrationRepositoryTests. The reader-mapping delegate passed to QuerySingleAsync
/// is never actually invoked by these mocks (the whole call is intercepted), so a real
/// MapPersonResponse is used purely to satisfy the constructor, same as RegistrationRepositoryTests
/// does with MapRegistrationResponses.
/// </summary>
[TestFixture]
public class PersonRepositoryTests
{
    private Mock<ISqlQueryExecutor> _sqlExecutorMock = null!;
    private IFixture _fixture = null!;

    [SetUp]
    public void Setup()
    {
        _fixture = AutoMoqFixture.Create();
        _sqlExecutorMock = new Mock<ISqlQueryExecutor>();
    }

    private PersonRepository CreateRepository() => new(
        _sqlExecutorMock.Object,
        new MapPersonResponse(),
        Mock.Of<ILogger<PersonRepository>>());

    private Person CreatePerson(string? emailAddress = null, string? cellPhoneNumber = null, string? firstName = null, string? lastName = null) =>
        new()
        {
            PersonId = _fixture.Create<int>(),
            PersonUuid = Guid.NewGuid(),
            EmailAddress = emailAddress ?? _fixture.Create<string>(),
            CellPhoneNumber = cellPhoneNumber ?? _fixture.Create<string>(),
            FirstName = firstName ?? _fixture.Create<string>(),
            LastName = lastName ?? _fixture.Create<string>(),
            IsActive = true,
            InsertedOn = DateTimeOffset.UtcNow,
            UpdatedOn = null
        };

    [Test]
    public void PersonRepository_Should_Implement_IPersonRepository()
    {
        CreateRepository().ShouldBeAssignableTo<IPersonRepository>();
    }

    [Test]
    public async Task FindOrCreateAsync_Should_ReturnExisting_When_ContactInformationMatches()
    {
        var expected = CreatePerson();
        _sqlExecutorMock
            .Setup(e => e.QuerySingleAsync(QueryPersons.GetByContactInformationSql, It.IsAny<Action<NpgsqlParameterCollection>>(), It.IsAny<Func<NpgsqlDataReader, Person>>()))
            .ReturnsAsync(expected);

        var result = await CreateRepository().FindOrCreateAsync(expected.FirstName, expected.LastName, expected.EmailAddress, expected.CellPhoneNumber);

        result.ShouldBe(expected);
        _sqlExecutorMock.Verify(e => e.QuerySingleAsync(QueryPersons.AddPersonSql, It.IsAny<Action<NpgsqlParameterCollection>>(), It.IsAny<Func<NpgsqlDataReader, Person>>()), Times.Never);
    }

    [Test]
    public async Task FindOrCreateAsync_Should_CreateNew_When_NoContactInformationMatch()
    {
        var created = CreatePerson();
        _sqlExecutorMock
            .Setup(e => e.QuerySingleAsync(QueryPersons.GetByContactInformationSql, It.IsAny<Action<NpgsqlParameterCollection>>(), It.IsAny<Func<NpgsqlDataReader, Person>>()))
            .ReturnsAsync((Person?)null);
        _sqlExecutorMock
            .Setup(e => e.QuerySingleAsync(QueryPersons.AddPersonSql, It.IsAny<Action<NpgsqlParameterCollection>>(), It.IsAny<Func<NpgsqlDataReader, Person>>()))
            .ReturnsAsync(created);

        var result = await CreateRepository().FindOrCreateAsync(created.FirstName, created.LastName, created.EmailAddress, created.CellPhoneNumber);

        result.ShouldBe(created);
    }

    [Test]
    public async Task FindOrCreateAsync_Should_ConfigureLookupParameters()
    {
        Action<NpgsqlParameterCollection>? captured = null;
        _sqlExecutorMock
            .Setup(e => e.QuerySingleAsync(QueryPersons.GetByContactInformationSql, It.IsAny<Action<NpgsqlParameterCollection>>(), It.IsAny<Func<NpgsqlDataReader, Person>>()))
            .Callback<string, Action<NpgsqlParameterCollection>, Func<NpgsqlDataReader, Person>>((_, configure, _) => captured = configure)
            .ReturnsAsync((Person?)null);
        var created = CreatePerson();
        _sqlExecutorMock
            .Setup(e => e.QuerySingleAsync(QueryPersons.AddPersonSql, It.IsAny<Action<NpgsqlParameterCollection>>(), It.IsAny<Func<NpgsqlDataReader, Person>>()))
            .ReturnsAsync(created);

        await CreateRepository().FindOrCreateAsync("Jane", "Doe", "jane@example.com", "555-1234");

        using var command = new NpgsqlCommand();
        captured!(command.Parameters);
        command.Parameters[pn.FirstName].Value.ShouldBe("Jane");
        command.Parameters[pn.LastName].Value.ShouldBe("Doe");
        command.Parameters[pn.EmailAddress].Value.ShouldBe("jane@example.com");
        command.Parameters[pn.CellPhoneNumber].Value.ShouldBe("555-1234");
    }

    [Test]
    public void FindOrCreateAsync_Should_Rethrow_When_ExecutorThrows()
    {
        _sqlExecutorMock
            .Setup(e => e.QuerySingleAsync(QueryPersons.GetByContactInformationSql, It.IsAny<Action<NpgsqlParameterCollection>>(), It.IsAny<Func<NpgsqlDataReader, Person>>()))
            .ThrowsAsync(new InvalidOperationException("boom"));

        Should.ThrowAsync<InvalidOperationException>(() => CreateRepository().FindOrCreateAsync("Jane", "Doe", "jane@example.com", "555-1234"));
    }
}
