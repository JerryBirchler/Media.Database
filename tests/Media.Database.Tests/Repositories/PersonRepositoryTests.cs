#nullable enable
using AutoFixture;
using Media.Common.Providers;
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
using System.Linq;
using System.Net;
using System.Threading.Tasks;
#pragma warning disable CS8981
using pn = Media.Database.Repositories.Schemas.ParameterNames;
#pragma warning restore CS8981

namespace Media.Database.Tests.Repositories;

/// <summary>
/// Covers PersonRepository's public API against a mocked ISqlQueryExecutor (and, for the
/// Scylla-first read path, a mocked ICqlQueryExecutor), following the same pattern as
/// FileRepositoryQueryTests. The reader-mapping delegate passed to QuerySingleAsync is never
/// actually invoked by these mocks (the whole call is intercepted), so a real MapPersonResponse
/// is used purely to satisfy the constructor, same as RegistrationRepositoryTests does with
/// MapRegistrationResponses.
/// </summary>
[TestFixture]
public class PersonRepositoryTests
{
    private Mock<ISqlQueryExecutor> _sqlExecutorMock = null!;
    private Mock<ICqlQueryExecutor> _cqlExecutorMock = null!;
    private Mock<IScyllaSessionProvider> _scyllaProviderMock = null!;
    private IFixture _fixture = null!;

    [SetUp]
    public void Setup()
    {
        _fixture = AutoMoqFixture.Create();
        _sqlExecutorMock = new Mock<ISqlQueryExecutor>();
        _cqlExecutorMock = new Mock<ICqlQueryExecutor>();
        _scyllaProviderMock = new Mock<IScyllaSessionProvider>();
    }

    private PersonRepository CreateRepository() => new(
        _sqlExecutorMock.Object,
        _cqlExecutorMock.Object,
        _scyllaProviderMock.Object,
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
            CreatedByPersonId = null,
            IsSuperAdmin = false,
            IsEmailVerified = false,
            IsSmsVerified = false,
            OtpWindowOverrideMinutes = null,
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

    [Test]
    public async Task GetByUuidAsync_Should_ReturnPerson_When_Found()
    {
        var expected = CreatePerson();
        _sqlExecutorMock
            .Setup(e => e.QuerySingleAsync(QueryPersons.GetByUuidSql, It.IsAny<Action<NpgsqlParameterCollection>>(), It.IsAny<Func<NpgsqlDataReader, Person>>()))
            .ReturnsAsync(expected);

        var result = await CreateRepository().GetByUuidAsync(expected.PersonUuid);

        result.ShouldBe(expected);
    }

    [Test]
    public async Task GetByUuidAsync_Should_ReturnNull_When_NotFound()
    {
        _sqlExecutorMock
            .Setup(e => e.QuerySingleAsync(QueryPersons.GetByUuidSql, It.IsAny<Action<NpgsqlParameterCollection>>(), It.IsAny<Func<NpgsqlDataReader, Person>>()))
            .ReturnsAsync((Person?)null);

        var result = await CreateRepository().GetByUuidAsync(Guid.NewGuid());

        result.ShouldBeNull();
    }

    [Test]
    public async Task GetByUuidAsync_Should_ConfigureLookupParameters()
    {
        Action<NpgsqlParameterCollection>? captured = null;
        var personUuid = Guid.NewGuid();
        _sqlExecutorMock
            .Setup(e => e.QuerySingleAsync(QueryPersons.GetByUuidSql, It.IsAny<Action<NpgsqlParameterCollection>>(), It.IsAny<Func<NpgsqlDataReader, Person>>()))
            .Callback<string, Action<NpgsqlParameterCollection>, Func<NpgsqlDataReader, Person>>((_, configure, _) => captured = configure)
            .ReturnsAsync((Person?)null);

        await CreateRepository().GetByUuidAsync(personUuid);

        using var command = new NpgsqlCommand();
        captured!(command.Parameters);
        command.Parameters[pn.PersonUuid].Value.ShouldBe(personUuid);
    }

    [Test]
    public void GetByUuidAsync_Should_Rethrow_When_ExecutorThrows()
    {
        _sqlExecutorMock
            .Setup(e => e.QuerySingleAsync(QueryPersons.GetByUuidSql, It.IsAny<Action<NpgsqlParameterCollection>>(), It.IsAny<Func<NpgsqlDataReader, Person>>()))
            .ThrowsAsync(new InvalidOperationException("boom"));

        Should.ThrowAsync<InvalidOperationException>(() => CreateRepository().GetByUuidAsync(Guid.NewGuid()));
    }

    [Test]
    public async Task CreateAsync_Should_ReturnCreatedPerson()
    {
        var created = CreatePerson();
        _sqlExecutorMock
            .Setup(e => e.QuerySingleAsync(QueryPersons.AddPersonWithCreatorSql, It.IsAny<Action<NpgsqlParameterCollection>>(), It.IsAny<Func<NpgsqlDataReader, Person>>()))
            .ReturnsAsync(created);

        var result = await CreateRepository().CreateAsync(created.FirstName, created.LastName, created.EmailAddress, created.CellPhoneNumber, createdByPersonId: 1);

        result.ShouldBe(created);
    }

    [Test]
    public async Task CreateAsync_Should_ConfigureParameters()
    {
        Action<NpgsqlParameterCollection>? captured = null;
        var created = CreatePerson();
        _sqlExecutorMock
            .Setup(e => e.QuerySingleAsync(QueryPersons.AddPersonWithCreatorSql, It.IsAny<Action<NpgsqlParameterCollection>>(), It.IsAny<Func<NpgsqlDataReader, Person>>()))
            .Callback<string, Action<NpgsqlParameterCollection>, Func<NpgsqlDataReader, Person>>((_, configure, _) => captured = configure)
            .ReturnsAsync(created);

        await CreateRepository().CreateAsync("Jane", "Doe", "jane@example.com", "555-1234", createdByPersonId: 7);

        using var command = new NpgsqlCommand();
        captured!(command.Parameters);
        command.Parameters[pn.FirstName].Value.ShouldBe("Jane");
        command.Parameters[pn.LastName].Value.ShouldBe("Doe");
        command.Parameters[pn.EmailAddress].Value.ShouldBe("jane@example.com");
        command.Parameters[pn.CellPhoneNumber].Value.ShouldBe("555-1234");
        command.Parameters[pn.CreatedByPersonId].Value.ShouldBe(7);
    }

    [Test]
    public void CreateAsync_Should_Rethrow_When_ExecutorThrows()
    {
        _sqlExecutorMock
            .Setup(e => e.QuerySingleAsync(QueryPersons.AddPersonWithCreatorSql, It.IsAny<Action<NpgsqlParameterCollection>>(), It.IsAny<Func<NpgsqlDataReader, Person>>()))
            .ThrowsAsync(new InvalidOperationException("boom"));

        Should.ThrowAsync<InvalidOperationException>(() => CreateRepository().CreateAsync("Jane", "Doe", "jane@example.com", "555-1234", 1));
    }

    [Test]
    public async Task GetPersonIdentifiersByCreatorIdAsync_Should_ReturnIdentifiers_From_Executor()
    {
        var expected = new List<PersonIdentifier> { _fixture.Create<PersonIdentifier>() };
        _sqlExecutorMock
            .Setup(e => e.QueryManyAsync(QueryPersons.GetPersonIdentifiersByCreatorSql, It.IsAny<Action<NpgsqlParameterCollection>>(), It.IsAny<Func<NpgsqlDataReader, PersonIdentifier>>()))
            .ReturnsAsync(expected);

        var result = await CreateRepository().GetPersonIdentifiersByCreatorIdAsync(1, next: null, limit: 5);

        result.ShouldBe(expected);
    }

    [Test]
    public async Task GetPersonIdentifiersByCreatorIdAsync_Should_ConfigureParameters()
    {
        Action<NpgsqlParameterCollection>? captured = null;
        var personUuid = Guid.NewGuid();
        var next = new PersonIdentifier { LastName = "Doe", FirstName = "Jane", PersonUuid = personUuid };
        _sqlExecutorMock
            .Setup(e => e.QueryManyAsync(QueryPersons.GetPersonIdentifiersByCreatorSql, It.IsAny<Action<NpgsqlParameterCollection>>(), It.IsAny<Func<NpgsqlDataReader, PersonIdentifier>>()))
            .Callback<string, Action<NpgsqlParameterCollection>, Func<NpgsqlDataReader, PersonIdentifier>>((_, configure, _) => captured = configure)
            .ReturnsAsync([]);

        await CreateRepository().GetPersonIdentifiersByCreatorIdAsync(1, next, limit: 5);

        using var command = new NpgsqlCommand();
        captured!(command.Parameters);
        command.Parameters[pn.CreatedByPersonId].Value.ShouldBe(1);
        command.Parameters[pn.LastName].Value.ShouldBe("Doe");
        command.Parameters[pn.FirstName].Value.ShouldBe("Jane");
        command.Parameters[pn.PersonUuid].Value.ShouldBe(personUuid);
        command.Parameters[pn.Limit].Value.ShouldBe(5);
    }

    [Test]
    public async Task GetPersonIdentifiersByCreatorIdAsync_Should_ConfigureCursorAsDbNull_When_FirstPage()
    {
        Action<NpgsqlParameterCollection>? captured = null;
        _sqlExecutorMock
            .Setup(e => e.QueryManyAsync(QueryPersons.GetPersonIdentifiersByCreatorSql, It.IsAny<Action<NpgsqlParameterCollection>>(), It.IsAny<Func<NpgsqlDataReader, PersonIdentifier>>()))
            .Callback<string, Action<NpgsqlParameterCollection>, Func<NpgsqlDataReader, PersonIdentifier>>((_, configure, _) => captured = configure)
            .ReturnsAsync([]);

        await CreateRepository().GetPersonIdentifiersByCreatorIdAsync(1, next: null, limit: 5);

        using var command = new NpgsqlCommand();
        captured!(command.Parameters);
        command.Parameters[pn.LastName].Value.ShouldBe(DBNull.Value);
        command.Parameters[pn.FirstName].Value.ShouldBe(DBNull.Value);
        command.Parameters[pn.PersonUuid].Value.ShouldBe(DBNull.Value);
    }

    [Test]
    public void GetPersonIdentifiersByCreatorIdAsync_Should_Rethrow_When_ExecutorThrows()
    {
        _sqlExecutorMock
            .Setup(e => e.QueryManyAsync(QueryPersons.GetPersonIdentifiersByCreatorSql, It.IsAny<Action<NpgsqlParameterCollection>>(), It.IsAny<Func<NpgsqlDataReader, PersonIdentifier>>()))
            .ThrowsAsync(new InvalidOperationException("boom"));

        Should.ThrowAsync<InvalidOperationException>(() => CreateRepository().GetPersonIdentifiersByCreatorIdAsync(1, null, 5));
    }

    [Test]
    public async Task GetAllPersonIdentifiersAsync_Should_ReturnIdentifiers_From_Executor()
    {
        var expected = new List<PersonIdentifier> { _fixture.Create<PersonIdentifier>() };
        _sqlExecutorMock
            .Setup(e => e.QueryManyAsync(QueryPersons.GetAllPersonIdentifiersSql, It.IsAny<Action<NpgsqlParameterCollection>>(), It.IsAny<Func<NpgsqlDataReader, PersonIdentifier>>()))
            .ReturnsAsync(expected);

        var result = await CreateRepository().GetAllPersonIdentifiersAsync(next: null, limit: 5);

        result.ShouldBe(expected);
    }

    [Test]
    public async Task GetAllPersonIdentifiersAsync_Should_ConfigureParameters()
    {
        Action<NpgsqlParameterCollection>? captured = null;
        var personUuid = Guid.NewGuid();
        var next = new PersonIdentifier { LastName = "Doe", FirstName = "Jane", PersonUuid = personUuid };
        _sqlExecutorMock
            .Setup(e => e.QueryManyAsync(QueryPersons.GetAllPersonIdentifiersSql, It.IsAny<Action<NpgsqlParameterCollection>>(), It.IsAny<Func<NpgsqlDataReader, PersonIdentifier>>()))
            .Callback<string, Action<NpgsqlParameterCollection>, Func<NpgsqlDataReader, PersonIdentifier>>((_, configure, _) => captured = configure)
            .ReturnsAsync([]);

        await CreateRepository().GetAllPersonIdentifiersAsync(next, limit: 5);

        using var command = new NpgsqlCommand();
        captured!(command.Parameters);
        command.Parameters[pn.LastName].Value.ShouldBe("Doe");
        command.Parameters[pn.FirstName].Value.ShouldBe("Jane");
        command.Parameters[pn.PersonUuid].Value.ShouldBe(personUuid);
        command.Parameters[pn.Limit].Value.ShouldBe(5);
    }

    [Test]
    public void GetAllPersonIdentifiersAsync_Should_Rethrow_When_ExecutorThrows()
    {
        _sqlExecutorMock
            .Setup(e => e.QueryManyAsync(QueryPersons.GetAllPersonIdentifiersSql, It.IsAny<Action<NpgsqlParameterCollection>>(), It.IsAny<Func<NpgsqlDataReader, PersonIdentifier>>()))
            .ThrowsAsync(new InvalidOperationException("boom"));

        Should.ThrowAsync<InvalidOperationException>(() => CreateRepository().GetAllPersonIdentifiersAsync(null, 5));
    }

    [Test]
    public async Task SetActiveAsync_Should_ReturnUpdatedPerson_When_Found()
    {
        var updated = CreatePerson();
        _sqlExecutorMock
            .Setup(e => e.QuerySingleAsync(QueryPersons.SetActiveSql, It.IsAny<Action<NpgsqlParameterCollection>>(), It.IsAny<Func<NpgsqlDataReader, Person>>()))
            .ReturnsAsync(updated);

        var result = await CreateRepository().SetActiveAsync(updated.PersonId, false);

        result.ShouldBe(updated);
    }

    [Test]
    public async Task SetActiveAsync_Should_ReturnNull_When_NotFound()
    {
        _sqlExecutorMock
            .Setup(e => e.QuerySingleAsync(QueryPersons.SetActiveSql, It.IsAny<Action<NpgsqlParameterCollection>>(), It.IsAny<Func<NpgsqlDataReader, Person>>()))
            .ReturnsAsync((Person?)null);

        var result = await CreateRepository().SetActiveAsync(1, true);

        result.ShouldBeNull();
    }

    [Test]
    public async Task UpdateAsync_Should_ReturnUpdatedPerson_When_Found()
    {
        var updated = CreatePerson();
        _sqlExecutorMock
            .Setup(e => e.QuerySingleAsync(QueryPersons.UpdateSql, It.IsAny<Action<NpgsqlParameterCollection>>(), It.IsAny<Func<NpgsqlDataReader, Person>>()))
            .ReturnsAsync(updated);

        var result = await CreateRepository().UpdateAsync(updated.PersonId, updated.FirstName, updated.LastName, updated.SpokenName, updated.EmailAddress, updated.CellPhoneNumber, isActive: true, isEmailVerified: true, isSmsVerified: true);

        result.ShouldBe(updated);
    }

    [Test]
    public async Task UpdateAsync_Should_ConfigureParameters()
    {
        Action<NpgsqlParameterCollection>? captured = null;
        var updated = CreatePerson();
        _sqlExecutorMock
            .Setup(e => e.QuerySingleAsync(QueryPersons.UpdateSql, It.IsAny<Action<NpgsqlParameterCollection>>(), It.IsAny<Func<NpgsqlDataReader, Person>>()))
            .Callback<string, Action<NpgsqlParameterCollection>, Func<NpgsqlDataReader, Person>>((_, configure, _) => captured = configure)
            .ReturnsAsync(updated);

        await CreateRepository().UpdateAsync(3, "Jane", "Doe", null, "jane@example.com", "555-1234", isActive: false, isEmailVerified: false, isSmsVerified: true);

        using var command = new NpgsqlCommand();
        captured!(command.Parameters);
        command.Parameters[pn.PersonId].Value.ShouldBe(3);
        command.Parameters[pn.FirstName].Value.ShouldBe("Jane");
        command.Parameters[pn.LastName].Value.ShouldBe("Doe");
        command.Parameters[pn.EmailAddress].Value.ShouldBe("jane@example.com");
        command.Parameters[pn.CellPhoneNumber].Value.ShouldBe("555-1234");
        command.Parameters[pn.IsActive].Value.ShouldBe(false);
        command.Parameters[pn.IsEmailVerified].Value.ShouldBe(false);
        command.Parameters[pn.IsSmsVerified].Value.ShouldBe(true);
    }

    [Test]
    public void UpdateAsync_Should_Rethrow_When_ExecutorThrows()
    {
        _sqlExecutorMock
            .Setup(e => e.QuerySingleAsync(QueryPersons.UpdateSql, It.IsAny<Action<NpgsqlParameterCollection>>(), It.IsAny<Func<NpgsqlDataReader, Person>>()))
            .ThrowsAsync(new InvalidOperationException("boom"));

        Should.ThrowAsync<InvalidOperationException>(() => CreateRepository().UpdateAsync(1, "Jane", "Doe", null, "jane@example.com", "555-1234", true, true, true));
    }

    [Test]
    public async Task SetVerifiedIfTrueAsync_Should_ReturnUpdatedPerson_When_Found()
    {
        var updated = CreatePerson();
        _sqlExecutorMock
            .Setup(e => e.QuerySingleAsync(QueryPersons.SetVerifiedIfTrueSql, It.IsAny<Action<NpgsqlParameterCollection>>(), It.IsAny<Func<NpgsqlDataReader, Person>>()))
            .ReturnsAsync(updated);

        var result = await CreateRepository().SetVerifiedIfTrueAsync(updated.PersonId, isEmailVerified: true, isSmsVerified: false);

        result.ShouldBe(updated);
    }

    [Test]
    public async Task SetVerifiedIfTrueAsync_Should_ConfigureParameters()
    {
        Action<NpgsqlParameterCollection>? captured = null;
        var updated = CreatePerson();
        _sqlExecutorMock
            .Setup(e => e.QuerySingleAsync(QueryPersons.SetVerifiedIfTrueSql, It.IsAny<Action<NpgsqlParameterCollection>>(), It.IsAny<Func<NpgsqlDataReader, Person>>()))
            .Callback<string, Action<NpgsqlParameterCollection>, Func<NpgsqlDataReader, Person>>((_, configure, _) => captured = configure)
            .ReturnsAsync(updated);

        await CreateRepository().SetVerifiedIfTrueAsync(3, isEmailVerified: true, isSmsVerified: false);

        using var command = new NpgsqlCommand();
        captured!(command.Parameters);
        command.Parameters[pn.PersonId].Value.ShouldBe(3);
        command.Parameters[pn.IsEmailVerified].Value.ShouldBe(true);
        command.Parameters[pn.IsSmsVerified].Value.ShouldBe(false);
    }

    [Test]
    public void SetVerifiedIfTrueAsync_Should_Rethrow_When_ExecutorThrows()
    {
        _sqlExecutorMock
            .Setup(e => e.QuerySingleAsync(QueryPersons.SetVerifiedIfTrueSql, It.IsAny<Action<NpgsqlParameterCollection>>(), It.IsAny<Func<NpgsqlDataReader, Person>>()))
            .ThrowsAsync(new InvalidOperationException("boom"));

        Should.ThrowAsync<InvalidOperationException>(() => CreateRepository().SetVerifiedIfTrueAsync(1, true, true));
    }

    [Test]
    public async Task GetByIdsAsync_Should_PreferScylla_When_RowFound()
    {
        var personId = _fixture.Create<int>();
        var scyllaPerson = CreatePerson() with { PersonId = personId };
        _cqlExecutorMock
            .Setup(e => e.QuerySingleAsync(QueryPersons.GetByIdCql, It.IsAny<Action<Dictionary<string, object>>>(), It.IsAny<Func<Cassandra.Row, Person>>()))
            .ReturnsAsync(scyllaPerson);

        var result = await CreateRepository().GetByIdsAsync([personId], maxDegreeOfParallelism: 1);

        result.ShouldBe([scyllaPerson]);
        _sqlExecutorMock.Verify(e => e.QuerySingleAsync(QueryPersons.GetByIdSql, It.IsAny<Action<NpgsqlParameterCollection>>(), It.IsAny<Func<NpgsqlDataReader, Person>>()), Times.Never);
    }

    [Test]
    public async Task GetByIdsAsync_Should_FallBackToPostgres_When_ScyllaHasNoRowYet()
    {
        var personId = _fixture.Create<int>();
        var postgresPerson = CreatePerson() with { PersonId = personId };
        _cqlExecutorMock
            .Setup(e => e.QuerySingleAsync(QueryPersons.GetByIdCql, It.IsAny<Action<Dictionary<string, object>>>(), It.IsAny<Func<Cassandra.Row, Person>>()))
            .ReturnsAsync((Person?)null);
        _sqlExecutorMock
            .Setup(e => e.QuerySingleAsync(QueryPersons.GetByIdSql, It.IsAny<Action<NpgsqlParameterCollection>>(), It.IsAny<Func<NpgsqlDataReader, Person>>()))
            .ReturnsAsync(postgresPerson);

        var result = await CreateRepository().GetByIdsAsync([personId], maxDegreeOfParallelism: 1);

        result.ShouldBe([postgresPerson]);
    }

    [Test]
    public async Task GetByIdsAsync_Should_FallBackToPostgres_And_AttemptHeal_When_ScyllaConnectivityExceptionThrown()
    {
        var personId = _fixture.Create<int>();
        var postgresPerson = CreatePerson() with { PersonId = personId };
        _cqlExecutorMock
            .Setup(e => e.QuerySingleAsync(QueryPersons.GetByIdCql, It.IsAny<Action<Dictionary<string, object>>>(), It.IsAny<Func<Cassandra.Row, Person>>()))
            .ThrowsAsync(new Cassandra.NoHostAvailableException(new Dictionary<IPEndPoint, Exception>()));
        _sqlExecutorMock
            .Setup(e => e.QuerySingleAsync(QueryPersons.GetByIdSql, It.IsAny<Action<NpgsqlParameterCollection>>(), It.IsAny<Func<NpgsqlDataReader, Person>>()))
            .ReturnsAsync(postgresPerson);
        _scyllaProviderMock.Setup(p => p.GetCurrentSessionId()).Returns(Guid.NewGuid());

        var result = await CreateRepository().GetByIdsAsync([personId], maxDegreeOfParallelism: 1);

        result.ShouldBe([postgresPerson]);
        _scyllaProviderMock.Verify(p => p.HealSessionAsync(It.IsAny<Guid>(), It.IsAny<string?>()), Times.Once);
    }

    [Test]
    public async Task GetByIdsAsync_Should_FallBackToPostgres_When_ScyllaThrowsNonConnectivityException()
    {
        var personId = _fixture.Create<int>();
        var postgresPerson = CreatePerson() with { PersonId = personId };
        _cqlExecutorMock
            .Setup(e => e.QuerySingleAsync(QueryPersons.GetByIdCql, It.IsAny<Action<Dictionary<string, object>>>(), It.IsAny<Func<Cassandra.Row, Person>>()))
            .ThrowsAsync(new InvalidOperationException("boom"));
        _sqlExecutorMock
            .Setup(e => e.QuerySingleAsync(QueryPersons.GetByIdSql, It.IsAny<Action<NpgsqlParameterCollection>>(), It.IsAny<Func<NpgsqlDataReader, Person>>()))
            .ReturnsAsync(postgresPerson);

        var result = await CreateRepository().GetByIdsAsync([personId], maxDegreeOfParallelism: 1);

        result.ShouldBe([postgresPerson]);
        _scyllaProviderMock.Verify(p => p.HealSessionAsync(It.IsAny<Guid>(), It.IsAny<string?>()), Times.Never);
    }

    [Test]
    public async Task GetByIdsAsync_Should_OmitId_When_NeitherStoreHasIt()
    {
        var personId = _fixture.Create<int>();
        _cqlExecutorMock
            .Setup(e => e.QuerySingleAsync(QueryPersons.GetByIdCql, It.IsAny<Action<Dictionary<string, object>>>(), It.IsAny<Func<Cassandra.Row, Person>>()))
            .ReturnsAsync((Person?)null);
        _sqlExecutorMock
            .Setup(e => e.QuerySingleAsync(QueryPersons.GetByIdSql, It.IsAny<Action<NpgsqlParameterCollection>>(), It.IsAny<Func<NpgsqlDataReader, Person>>()))
            .ReturnsAsync((Person?)null);

        var result = await CreateRepository().GetByIdsAsync([personId], maxDegreeOfParallelism: 1);

        result.ShouldBeEmpty();
    }

    [Test]
    public async Task GetByIdsAsync_Should_HydrateEveryId_When_MultipleIdsRequested()
    {
        var personIds = _fixture.CreateMany<int>(4).ToList();
        _cqlExecutorMock
            .Setup(e => e.QuerySingleAsync(QueryPersons.GetByIdCql, It.IsAny<Action<Dictionary<string, object>>>(), It.IsAny<Func<Cassandra.Row, Person>>()))
            .ReturnsAsync((string _, Action<Dictionary<string, object>> configure, Func<Cassandra.Row, Person> _) =>
            {
                var parameters = new Dictionary<string, object>();
                configure(parameters);
                var personId = (int)parameters[pn.PersonId.ToUpperInvariant()];
                return CreatePerson() with { PersonId = personId };
            });

        var result = await CreateRepository().GetByIdsAsync(personIds, maxDegreeOfParallelism: 2);

        result.Select(p => p.PersonId).ShouldBe(personIds, ignoreOrder: true);
    }
}
