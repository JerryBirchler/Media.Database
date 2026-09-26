#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using AutoFixture;
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

namespace Media.Database.Tests.Repositories;

/// <summary>
/// Executing a search, kept apart from the storage tests because the interesting property is not
/// what is written but what is allowed to run.
/// </summary>
[TestFixture]
public class SearchListRepositorySearchTests
{
    private const int PersonId = 42;

    private static readonly SearchListScopeSchema PersonSchema = SearchListScopeSchema.For(OwnerScope.Person);

    private Mock<ISqlQueryExecutor> _sqlExecutorMock = null!;
    private Mock<ICqlQueryExecutor> _cqlExecutorMock = null!;
    private Mock<ISearchListCipher> _cipherMock = null!;

    [SetUp]
    public void Setup()
    {
        _sqlExecutorMock = new Mock<ISqlQueryExecutor>();
        _cqlExecutorMock = new Mock<ICqlQueryExecutor>();
        _cipherMock = new Mock<ISearchListCipher>();

        _cipherMock.Setup(c => c.Encrypt(It.IsAny<string>())).Returns<string>(v => "enc:" + v);
        _cipherMock.Setup(c => c.Decrypt(It.IsAny<string>())).Returns<string>(v => v["enc:".Length..]);
    }

    private SearchListRepository CreateRepository() =>
        new(_sqlExecutorMock.Object, _cqlExecutorMock.Object, _cipherMock.Object,
            Mock.Of<ILogger<SearchListRepository>>());

    private static SearchList AnOrList(int id, string name, params string[] words) => new()
    {
        Id = id,
        Uuid = Guid.NewGuid(),
        Scope = OwnerScope.Person,
        OwnerId = PersonId,
        ListType = SearchListType.Or,
        Name = name,
        Lines = words.Select(word => new SearchListLine { LookingFor = word }).ToList(),
    };

    private static SearchList AnAndList(int id, string name, params Guid[] references) => new()
    {
        Id = id,
        Uuid = Guid.NewGuid(),
        Scope = OwnerScope.Person,
        OwnerId = PersonId,
        ListType = SearchListType.And,
        Name = name,
        References = references,
    };

    /// <summary>Everything the caller owns, as the two reads that assemble it.</summary>
    private void OwnedLists(params SearchList[] lists)
    {
        _sqlExecutorMock
            .Setup(e => e.QueryManyAsync(QuerySearchLists.GetAll(PersonSchema),
                It.IsAny<Action<NpgsqlParameterCollection>>(),
                It.IsAny<Func<NpgsqlDataReader, SearchList>>()))
            .ReturnsAsync(lists.ToList());

        var contents = lists.Select(list => new SearchListContent
        {
            Id = list.Id,
            Name = "enc:" + list.Name,
            Payload = "enc:" + JsonSerializer.Serialize(new SearchListPayload
            {
                Lines = list.Lines,
                References = list.References,
            }),
            PayloadVersion = 1,
        }).ToList();

        _cqlExecutorMock
            .Setup(e => e.QueryManyAsync(QuerySearchListsCql.GetAll(PersonSchema),
                It.IsAny<Action<Dictionary<string, object>>>(),
                It.IsAny<Func<Row, SearchListContent>>()))
            .ReturnsAsync(contents);
    }

    private void SearchReturns(params FileSearchResult[] results)
    {
        _sqlExecutorMock
            .Setup(e => e.QueryManyAsync(It.Is<string>(sql => sql.Contains("GROUP BY")),
                It.IsAny<Action<NpgsqlParameterCollection>>(),
                It.IsAny<Func<NpgsqlDataReader, FileSearchResult>>()))
            .ReturnsAsync(results.ToList());
    }

    /// <summary>How many lists the statement ended up requiring: one bool_or per list.</summary>
    private void ShouldHaveRequiredLists(int count)
    {
        _sqlExecutorMock.Verify(e => e.QueryManyAsync(
            It.Is<string>(sql => sql.Split("bool_or").Length == count + 1),
            It.IsAny<Action<NpgsqlParameterCollection>>(),
            It.IsAny<Func<NpgsqlDataReader, FileSearchResult>>()), Times.Once);
    }

    private Task<IReadOnlyList<FileSearchResult>> Search(
        IReadOnlyList<Guid> lists, IReadOnlyList<SearchListLine>? adHoc = null) =>
        CreateRepository().SearchFilesAsync(OwnerScope.Person, PersonId, lists, adHoc ?? [], true, [], 100);

    [Test]
    public async Task SearchFilesAsync_Should_RunOnlyTheListsTheCallerOwns()
    {
        var mine = AnOrList(1, "the girls", "Belle");
        OwnedLists(mine);
        SearchReturns();

        // A uuid the caller does not own is simply absent from the owned set, so it cannot be
        // run. That is what makes running somebody else's list impossible rather than merely
        // forbidden: a search that executes another person's list is a decryption oracle, even
        // though nothing is ever decrypted.
        await Search([mine.Uuid, Guid.NewGuid()]);

        ShouldHaveRequiredLists(1);
    }

    [Test]
    public async Task SearchFilesAsync_Should_ExpandAnAndListIntoWhatItReferences()
    {
        var girls = AnOrList(1, "the girls", "Belle");
        var beaches = AnOrList(2, "beaches", "beach");
        var both = AnAndList(3, "the girls at the beach", girls.Uuid, beaches.Uuid);
        OwnedLists(girls, beaches, both);
        SearchReturns();

        await Search([both.Uuid]);

        ShouldHaveRequiredLists(2);
    }

    [Test]
    public async Task SearchFilesAsync_Should_SkipAReferenceTheCallerNoLongerOwns()
    {
        var girls = AnOrList(1, "the girls", "Belle");
        var stale = AnAndList(3, "stale", girls.Uuid, Guid.NewGuid());
        OwnedLists(girls, stale);
        SearchReturns();

        // References are resolved at execution rather than trusted from when the And list was
        // saved, because ownership can change underneath them.
        await Search([stale.Uuid]);

        ShouldHaveRequiredLists(1);
    }

    [Test]
    public async Task SearchFilesAsync_Should_AndAnAdHocLineWithTheNamedLists()
    {
        var girls = AnOrList(1, "the girls", "Belle");
        OwnedLists(girls);
        SearchReturns();

        // A typed-in search is one more list to satisfy, not a special case.
        await Search([girls.Uuid], [new SearchListLine { LookingFor = "beach" }]);

        ShouldHaveRequiredLists(2);
    }

    [Test]
    public async Task SearchFilesAsync_Should_SearchOnAnAdHocLineAlone()
    {
        OwnedLists();
        SearchReturns();

        await Search([], [new SearchListLine { LookingFor = "beach" }]);

        ShouldHaveRequiredLists(1);
    }

    [Test]
    public async Task SearchFilesAsync_Should_SkipAListWithNoLines()
    {
        var empty = AnOrList(1, "empty");
        var girls = AnOrList(2, "the girls", "Belle");
        OwnedLists(empty, girls);
        SearchReturns();

        // Vacuously true rather than matching nothing: an emptied list must not silently return
        // zero rows.
        await Search([empty.Uuid, girls.Uuid]);

        ShouldHaveRequiredLists(1);
    }

    [Test]
    public async Task SearchFilesAsync_Should_ReturnWhatTheQueryFound()
    {
        var girls = AnOrList(1, "the girls", "Belle");
        OwnedLists(girls);

        var hit = new FileSearchResult
        {
            FileId = Guid.NewGuid(),
            OriginalFilePath = "D:/Media/2019/belle.txt",
            IsCurrent = true,
            SourceMachineId = 17,
        };
        SearchReturns(hit);

        var results = await Search([girls.Uuid]);

        results.ShouldHaveSingleItem().OriginalFilePath.ShouldBe(hit.OriginalFilePath);
    }

    [Test]
    public async Task SearchFilesAsync_Should_ReturnNothing_When_TheCallerOwnsNoListsAtAll()
    {
        OwnedLists();
        SearchReturns();

        var results = await Search([Guid.NewGuid()]);

        results.ShouldBeEmpty();
    }
}
