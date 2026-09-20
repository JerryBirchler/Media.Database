#nullable enable
using Media.Database.Models;
using Media.Database.Repositories;
using Media.Database.Repositories.Queries;
using Microsoft.Extensions.Logging;
using Moq;
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
public class GroupUuidOrchestrationRepositoryTests
{
    private Mock<ICqlQueryExecutor> _cqlExecutorMock = null!;

    [SetUp]
    public void Setup()
    {
        _cqlExecutorMock = new Mock<ICqlQueryExecutor>();
    }

    private GroupUuidOrchestrationRepository CreateRepository() => new(
        _cqlExecutorMock.Object,
        Mock.Of<ILogger<GroupUuidOrchestrationRepository>>());

    [Test]
    public void GroupUuidOrchestrationRepository_Should_Implement_IGroupUuidOrchestrationRepository()
    {
        CreateRepository().ShouldBeAssignableTo<IGroupUuidOrchestrationRepository>();
    }

    [Test]
    public async Task UpsertAsync_Should_ConfigureParameters()
    {
        Action<Dictionary<string, object>>? captured = null;
        _cqlExecutorMock
            .Setup(e => e.ExecuteAsync(QueryGroupUuidOrchestration.UpsertSql, It.IsAny<Action<Dictionary<string, object>>>()))
            .Callback<string, Action<Dictionary<string, object>>>((_, configure) => captured = configure);
        var personUuid = Guid.NewGuid();
        var keyUuid = Guid.NewGuid();

        await CreateRepository().UpsertAsync(personUuid, 3, "encrypted-blob", keyUuid);

        var parameters = new Dictionary<string, object>();
        captured!(parameters);
        parameters[pn.PersonUuid.ToUpperInvariant()].ShouldBe(personUuid);
        parameters[pn.GroupShellId.ToUpperInvariant()].ShouldBe(3);
        parameters[pn.EncryptedUuidBlob.ToUpperInvariant()].ShouldBe("encrypted-blob");
        parameters[pn.GroupEncryptionKeyUuid.ToUpperInvariant()].ShouldBe(keyUuid);
    }

    [Test]
    public void UpsertAsync_Should_Rethrow_When_ExecutorThrows()
    {
        _cqlExecutorMock
            .Setup(e => e.ExecuteAsync(QueryGroupUuidOrchestration.UpsertSql, It.IsAny<Action<Dictionary<string, object>>>()))
            .ThrowsAsync(new InvalidOperationException("boom"));

        Should.ThrowAsync<InvalidOperationException>(() => CreateRepository().UpsertAsync(Guid.NewGuid(), 3, "blob", Guid.NewGuid()));
    }

    [Test]
    public async Task GetAllByPersonUuidAsync_Should_ReturnEntries_From_Executor()
    {
        var personUuid = Guid.NewGuid();
        var expected = new List<GroupUuidOrchestration>
        {
            new()
            {
                PersonUuid = personUuid,
                GroupShellId = 3,
                EncryptedUuidBlob = "blob",
                GroupEncryptionKeyUuid = Guid.NewGuid(),
                UpdatedOn = DateTimeOffset.UtcNow
            }
        };
        _cqlExecutorMock
            .Setup(e => e.QueryManyAsync(QueryGroupUuidOrchestration.GetAllByPersonUuidSql, It.IsAny<Action<Dictionary<string, object>>>(), It.IsAny<Func<Cassandra.Row, GroupUuidOrchestration>>()))
            .ReturnsAsync(expected);

        var result = await CreateRepository().GetAllByPersonUuidAsync(personUuid);

        result.ShouldBe(expected);
    }

    [Test]
    public async Task GetAllByPersonUuidAsync_Should_ConfigurePersonUuidParameter()
    {
        Action<Dictionary<string, object>>? captured = null;
        _cqlExecutorMock
            .Setup(e => e.QueryManyAsync(QueryGroupUuidOrchestration.GetAllByPersonUuidSql, It.IsAny<Action<Dictionary<string, object>>>(), It.IsAny<Func<Cassandra.Row, GroupUuidOrchestration>>()))
            .Callback<string, Action<Dictionary<string, object>>, Func<Cassandra.Row, GroupUuidOrchestration>>((_, configure, _) => captured = configure)
            .ReturnsAsync([]);
        var personUuid = Guid.NewGuid();

        await CreateRepository().GetAllByPersonUuidAsync(personUuid);

        var parameters = new Dictionary<string, object>();
        captured!(parameters);
        parameters[pn.PersonUuid.ToUpperInvariant()].ShouldBe(personUuid);
    }

    [Test]
    public void GetAllByPersonUuidAsync_Should_Rethrow_When_ExecutorThrows()
    {
        _cqlExecutorMock
            .Setup(e => e.QueryManyAsync(QueryGroupUuidOrchestration.GetAllByPersonUuidSql, It.IsAny<Action<Dictionary<string, object>>>(), It.IsAny<Func<Cassandra.Row, GroupUuidOrchestration>>()))
            .ThrowsAsync(new InvalidOperationException("boom"));

        Should.ThrowAsync<InvalidOperationException>(() => CreateRepository().GetAllByPersonUuidAsync(Guid.NewGuid()));
    }
}
