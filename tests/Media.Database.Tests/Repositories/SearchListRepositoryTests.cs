using AutoFixture;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Cassandra;
using Media.Database.Models;
using Media.Database.Repositories;
using Media.Database.Repositories.Queries;
using Media.Database.Tests.TestHelpers;
using Microsoft.Extensions.Logging;
using Moq;
using Npgsql;
using NUnit.Framework;
using Shouldly;
using System.Text.Json;

namespace Media.Database.Tests.Repositories;

[TestFixture]
public class SearchListRepositoryTests
{
    private Mock<ISqlQueryExecutor> _sqlExecutorMock = null!;
    private Mock<ICqlQueryExecutor> _cqlExecutorMock = null!;
    private Mock<ISearchListCipher> _cipherMock = null!;
    private IFixture _fixture = null!;

    private const int PersonId = 42;
    private const int GroupId = 7;
    private const int ListId = 99;

    [SetUp]
    public void Setup()
    {
        _fixture = AutoMoqFixture.Create();
        _sqlExecutorMock = new Mock<ISqlQueryExecutor>();
        _cqlExecutorMock = new Mock<ICqlQueryExecutor>();
        _cipherMock = new Mock<ISearchListCipher>();

        // A reversible stand-in for real cryptography. The seam exists precisely so these tests
        // are about storage rather than about AES.
        _cipherMock.Setup(c => c.Encrypt(It.IsAny<string>())).Returns<string>(v => $"enc:{v}");
        _cipherMock.Setup(c => c.Decrypt(It.IsAny<string>())).Returns<string>(v => v["enc:".Length..]);
    }

    private SearchListRepository CreateRepository() =>
        new(_sqlExecutorMock.Object, _cqlExecutorMock.Object, _cipherMock.Object,
            Mock.Of<ILogger<SearchListRepository>>());

    private static SearchList ASkeleton(OwnerScope scope, int ownerId) => new()
    {
        Id = ListId,
        Uuid = Guid.NewGuid(),
        Scope = scope,
        OwnerId = ownerId,
        ListType = SearchListType.Or
    };

    private void SqlReturns(string sql, SearchList? list) =>
        _sqlExecutorMock
            .Setup(e => e.QuerySingleAsync(sql,
                It.IsAny<Action<NpgsqlParameterCollection>>(),
                It.IsAny<Func<NpgsqlDataReader, SearchList>>()))
            .ReturnsAsync(list);

    /// <summary>
    /// Runs the parameter-configuring lambda and captures what it wrote. The real executor is
    /// what normally invokes it, so without this the cipher is never called and the assertions
    /// silently pass on nothing.
    /// </summary>
    private Dictionary<string, object> CaptureCqlParameters()
    {
        var captured = new Dictionary<string, object>();

        _cqlExecutorMock
            .Setup(e => e.ExecuteAsync(It.IsAny<string>(), It.IsAny<Action<Dictionary<string, object>>>()))
            .Callback<string, Action<Dictionary<string, object>>>((_, configure) => configure(captured))
            .Returns(Task.CompletedTask);

        return captured;
    }

    private static IReadOnlyList<SearchListLine> Lines() =>
    [
        new SearchListLine { LookingFor = "Belle" },
        new SearchListLine { LookingFor = "Betty Sue", MetadataType = WordOrigin.Name }
    ];

    [Test]
    public async Task AddAsync_Should_WriteTheContentUnderTheIdentityPostgresIssued()
    {
        var skeleton = ASkeleton(OwnerScope.Person, PersonId);
        SqlReturns(QuerySearchLists.AddPersonSql, skeleton);

        var result = await CreateRepository()
            .AddAsync(OwnerScope.Person, PersonId, SearchListType.Or, "the girls", Lines());

        result.ShouldNotBeNull();
        result.Uuid.ShouldBe(skeleton.Uuid);

        _cqlExecutorMock.Verify(e => e.ExecuteAsync(
            QuerySearchListsCql.UpsertPersonSql,
            It.IsAny<Action<Dictionary<string, object>>>()), Times.Once);
    }

    [Test]
    public async Task AddAsync_Should_EncryptBothTheNameAndTheLines()
    {
        SqlReturns(QuerySearchLists.AddPersonSql, ASkeleton(OwnerScope.Person, PersonId));
        var parameters = CaptureCqlParameters();

        await CreateRepository()
            .AddAsync(OwnerScope.Person, PersonId, SearchListType.Or, "the girls", Lines());

        // Two calls, not one: the name is encrypted as well as the payload. Exempting it would be
        // the situational reasoning the design rejects.
        _cipherMock.Verify(c => c.Encrypt("the girls"), Times.Once);
        _cipherMock.Verify(c => c.Encrypt(It.IsAny<string>()), Times.Exactly(2));

        // And nothing reaches Scylla in the clear.
        parameters.Values.OfType<string>().ShouldAllBe(v => v.StartsWith("enc:"));
    }

    [Test]
    public async Task AddAsync_Should_SerializeTheLinesWithoutTheirDefaults()
    {
        SqlReturns(QuerySearchLists.AddPersonSql, ASkeleton(OwnerScope.Person, PersonId));
        CaptureCqlParameters();

        string? payload = null;
        _cipherMock.Setup(c => c.Encrypt(It.Is<string>(v => v.StartsWith('{'))))
            .Returns<string>(v => { payload = v; return $"enc:{v}"; });

        await CreateRepository()
            .AddAsync(OwnerScope.Person, PersonId, SearchListType.Or, "the girls", Lines());

        payload.ShouldNotBeNull();

        // Absent means any, and version 1 is what says so. Stamping the defaults into every row
        // would put the same rule in two places.
        payload.ShouldNotContain("wordType");
        payload.ShouldContain("\"lookingFor\":\"Belle\"");
        payload.ShouldContain("\"metadataType\":\"Name\"");
    }

    [Test]
    public async Task AddAsync_Should_ReturnNullAndWriteNothing_When_PostgresCreatedNoRow()
    {
        SqlReturns(QuerySearchLists.AddPersonSql, null);

        var result = await CreateRepository()
            .AddAsync(OwnerScope.Person, PersonId, SearchListType.Or, "the girls", Lines());

        result.ShouldBeNull();
        _cqlExecutorMock.Verify(e => e.ExecuteAsync(
            It.IsAny<string>(), It.IsAny<Action<Dictionary<string, object>>>()), Times.Never);
    }

    [Test]
    public async Task AddAsync_Should_UseTheGroupStatements_When_ScopeIsGroup()
    {
        SqlReturns(QuerySearchLists.AddGroupSql, ASkeleton(OwnerScope.Group, GroupId));

        await CreateRepository()
            .AddAsync(OwnerScope.Group, GroupId, SearchListType.Or, "the girls", Lines());

        _cqlExecutorMock.Verify(e => e.ExecuteAsync(
            QuerySearchListsCql.UpsertGroupSql,
            It.IsAny<Action<Dictionary<string, object>>>()), Times.Once);
    }

    [Test]
    public async Task GetAsync_Should_ReturnNullWithoutTouchingScylla_When_TheListIsNotTheCallers()
    {
        SqlReturns(QuerySearchLists.GetPersonByUuidSql, null);

        var result = await CreateRepository().GetAsync(OwnerScope.Person, PersonId, Guid.NewGuid());

        // The owner predicate in the SQL is the authorization check, so a miss must stop here --
        // reaching Scylla anyway would leak that the row exists.
        result.ShouldBeNull();
        _cqlExecutorMock.Verify(e => e.QuerySingleAsync(
            It.IsAny<string>(),
            It.IsAny<Action<Dictionary<string, object>>>(),
            It.IsAny<Func<Row, SearchListContent>>()), Times.Never);
    }

    [Test]
    public async Task GetAsync_Should_DecryptTheNameAndTheLines()
    {
        SqlReturns(QuerySearchLists.GetPersonByUuidSql, ASkeleton(OwnerScope.Person, PersonId));

        var payload = JsonSerializer.Serialize(new SearchListPayload { Lines = Lines() });

        _cqlExecutorMock
            .Setup(e => e.QuerySingleAsync(QuerySearchListsCql.GetPersonSql,
                It.IsAny<Action<Dictionary<string, object>>>(),
                It.IsAny<Func<Row, SearchListContent>>()))
            .ReturnsAsync(new SearchListContent
            {
                Id = ListId,
                Name = "enc:the girls",
                Payload = $"enc:{payload}",
                PayloadVersion = 1
            });

        var result = await CreateRepository().GetAsync(OwnerScope.Person, PersonId, Guid.NewGuid());

        result.ShouldNotBeNull();
        result.Name.ShouldBe("the girls");
        result.PayloadVersion.ShouldBe(1);
        result.Lines.Select(l => l.LookingFor).ShouldBe(["Belle", "Betty Sue"]);
        result.Lines[1].MetadataType.ShouldBe(WordOrigin.Name);

        // Absent on the wire means any, and must survive the round trip as null.
        result.Lines[0].MetadataType.ShouldBeNull();
        result.Lines[0].WordType.ShouldBeNull();
    }

    [Test]
    public async Task GetAsync_Should_ReturnTheListNameless_When_TheContentRowIsMissing()
    {
        SqlReturns(QuerySearchLists.GetPersonByUuidSql, ASkeleton(OwnerScope.Person, PersonId));

        _cqlExecutorMock
            .Setup(e => e.QuerySingleAsync(It.IsAny<string>(),
                It.IsAny<Action<Dictionary<string, object>>>(),
                It.IsAny<Func<Row, SearchListContent>>()))
            .ReturnsAsync((SearchListContent?)null);

        var result = await CreateRepository().GetAsync(OwnerScope.Person, PersonId, Guid.NewGuid());

        // A half-finished two-store write is not something the caller can act on, so the gap is
        // visible in the response rather than raised as a 500.
        result.ShouldNotBeNull();
        result.Name.ShouldBeEmpty();
        result.Lines.ShouldBeEmpty();
    }

    [Test]
    public async Task GetAllAsync_Should_HydrateEachSkeletonFromOnePartitionRead()
    {
        var first = ASkeleton(OwnerScope.Person, PersonId);
        var second = ASkeleton(OwnerScope.Person, PersonId);
        second.Id = ListId + 1;

        _sqlExecutorMock
            .Setup(e => e.QueryManyAsync(QuerySearchLists.GetPersonListsSql,
                It.IsAny<Action<NpgsqlParameterCollection>>(),
                It.IsAny<Func<NpgsqlDataReader, SearchList>>()))
            .ReturnsAsync([first, second]);

        var payload = $"enc:{JsonSerializer.Serialize(new SearchListPayload { Lines = Lines() })}";

        _cqlExecutorMock
            .Setup(e => e.QueryManyAsync(QuerySearchListsCql.GetAllForPersonSql,
                It.IsAny<Action<Dictionary<string, object>>>(),
                It.IsAny<Func<Row, SearchListContent>>()))
            .ReturnsAsync(
            [
                new SearchListContent { Id = first.Id, Name = "enc:the girls", Payload = payload, PayloadVersion = 1 },
                new SearchListContent { Id = second.Id, Name = "enc:beaches", Payload = payload, PayloadVersion = 1 }
            ]);

        var result = await CreateRepository().GetAllAsync(OwnerScope.Person, PersonId);

        result.Select(l => l.Name).ShouldBe(["the girls", "beaches"]);

        // One read for every name the picker needs, not one per list.
        _cqlExecutorMock.Verify(e => e.QueryManyAsync(
            It.IsAny<string>(),
            It.IsAny<Action<Dictionary<string, object>>>(),
            It.IsAny<Func<Row, SearchListContent>>()), Times.Once);
    }

    [Test]
    public async Task GetAllAsync_Should_ReturnEmptyWithoutReachingScylla_When_TheOwnerHasNoLists()
    {
        _sqlExecutorMock
            .Setup(e => e.QueryManyAsync(It.IsAny<string>(),
                It.IsAny<Action<NpgsqlParameterCollection>>(),
                It.IsAny<Func<NpgsqlDataReader, SearchList>>()))
            .ReturnsAsync([]);

        (await CreateRepository().GetAllAsync(OwnerScope.Person, PersonId)).ShouldBeEmpty();

        _cqlExecutorMock.Verify(e => e.QueryManyAsync(
            It.IsAny<string>(),
            It.IsAny<Action<Dictionary<string, object>>>(),
            It.IsAny<Func<Row, SearchListContent>>()), Times.Never);
    }

    [Test]
    public async Task UpdateAsync_Should_LeaveScyllaUntouched_When_TheListIsNotTheCallers()
    {
        SqlReturns(QuerySearchLists.UpdatePersonSql, null);

        var result = await CreateRepository().UpdateAsync(
            OwnerScope.Person, PersonId, Guid.NewGuid(), SearchListType.Or, "the girls", Lines());

        // The Postgres update is the authorization check, so a rejected update must not rewrite
        // somebody else's blobs.
        result.ShouldBeNull();
        _cqlExecutorMock.Verify(e => e.ExecuteAsync(
            It.IsAny<string>(), It.IsAny<Action<Dictionary<string, object>>>()), Times.Never);
    }

    [Test]
    public async Task UpdateAsync_Should_RewriteTheContent_When_TheListIsTheCallers()
    {
        SqlReturns(QuerySearchLists.UpdatePersonSql, ASkeleton(OwnerScope.Person, PersonId));

        var result = await CreateRepository().UpdateAsync(
            OwnerScope.Person, PersonId, Guid.NewGuid(), SearchListType.Or, "the girls", Lines());

        result.ShouldNotBeNull();
        result.Name.ShouldBe("the girls");

        _cqlExecutorMock.Verify(e => e.ExecuteAsync(
            QuerySearchListsCql.UpsertPersonSql,
            It.IsAny<Action<Dictionary<string, object>>>()), Times.Once);
    }

    [Test]
    public async Task DeleteAsync_Should_RemoveFromBothStores_When_TheListIsTheCallers()
    {
        _sqlExecutorMock
            .Setup(e => e.QuerySingleValueAsync(QuerySearchLists.DeletePersonSql,
                It.IsAny<Action<NpgsqlParameterCollection>>(),
                It.IsAny<Func<NpgsqlDataReader, int>>()))
            .ReturnsAsync(ListId);

        (await CreateRepository().DeleteAsync(OwnerScope.Person, PersonId, Guid.NewGuid())).ShouldBeTrue();

        _cqlExecutorMock.Verify(e => e.ExecuteAsync(
            QuerySearchListsCql.DeletePersonSql,
            It.IsAny<Action<Dictionary<string, object>>>()), Times.Once);
    }

    [Test]
    public async Task DeleteAsync_Should_ReturnFalseAndRemoveNothing_When_TheListIsNotTheCallers()
    {
        _sqlExecutorMock
            .Setup(e => e.QuerySingleValueAsync(It.IsAny<string>(),
                It.IsAny<Action<NpgsqlParameterCollection>>(),
                It.IsAny<Func<NpgsqlDataReader, int>>()))
            .ReturnsAsync((int?)null);

        (await CreateRepository().DeleteAsync(OwnerScope.Person, PersonId, Guid.NewGuid())).ShouldBeFalse();

        _cqlExecutorMock.Verify(e => e.ExecuteAsync(
            It.IsAny<string>(), It.IsAny<Action<Dictionary<string, object>>>()), Times.Never);
    }
}
